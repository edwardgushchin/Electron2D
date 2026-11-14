#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using Electron2D.NVorbisBindings.Contracts;
using System;

namespace Electron2D.NVorbisBindings
{
    class Mapping : IMapping
    {
        IMdct _mdct;
        int[] _couplingAngle;
        int[] _couplingMangitude;
        IResidue[] _submapResidue;
        IFloor[] _channelFloor;
        int[][] _submapChannels;

        public void Init(IPacket packet, int channels, IFloor[] floors, IResidue[] residues, IMdct mdct)
        {
            var submapCount = 1;
            if (packet.ReadBit())
            {
                submapCount += (int)packet.ReadBits(4);
            }

            // square polar mapping
            var couplingSteps = 0;
            if (packet.ReadBit())
            {
                couplingSteps = (int)packet.ReadBits(8) + 1;
            }

            var couplingBits = Utils.ilog(channels - 1);
            _couplingAngle = new int[couplingSteps];
            _couplingMangitude = new int[couplingSteps];
            for (var j = 0; j < couplingSteps; j++)
            {
                var magnitude = (int)packet.ReadBits(couplingBits);
                var angle = (int)packet.ReadBits(couplingBits);
                if (magnitude == angle || magnitude > channels - 1 || angle > channels - 1)
                {
                    throw new System.IO.InvalidDataException("Invalid magnitude or angle in mapping header!");
                }
                _couplingAngle[j] = angle;
                _couplingMangitude[j] = magnitude;
            }

            if (0 != packet.ReadBits(2))
            {
                throw new System.IO.InvalidDataException("Reserved bits not 0 in mapping header.");
            }

            var mux = new int[channels];
            if (submapCount > 1)
            {
                for (var c = 0; c < channels; c++)
                {
                    mux[c] = (int)packet.ReadBits(4);
                    if (mux[c] >= submapCount)
                    {
                        throw new System.IO.InvalidDataException("Invalid channel mux submap index in mapping header!");
                    }
                }
            }

            var submapFloor = new IFloor[submapCount];
            _submapResidue = new IResidue[submapCount];
            for (var j = 0; j < submapCount; j++)
            {
                packet.SkipBits(8); // unused placeholder
                var floorNum = (int)packet.ReadBits(8);
                if (floorNum >= floors.Length)
                {
                    throw new System.IO.InvalidDataException("Invalid floor number in mapping header!");
                }
                var residueNum = (int)packet.ReadBits(8);
                if (residueNum >= residues.Length)
                {
                    throw new System.IO.InvalidDataException("Invalid residue number in mapping header!");
                }

                submapFloor[j] = floors[floorNum];
                _submapResidue[j] = residues[residueNum];
            }

            _channelFloor = new IFloor[channels];
            for (var c = 0; c < channels; c++)
            {
                _channelFloor[c] = submapFloor[mux[c]];
            }

            _submapChannels = new int[submapCount][];
            var submapSizes = new int[submapCount];
            for (var c = 0; c < channels; c++)
            {
                submapSizes[mux[c]]++;
            }
            for (var i = 0; i < submapCount; i++)
            {
                _submapChannels[i] = new int[submapSizes[i]];
            }
            Array.Clear(submapSizes, 0, submapSizes.Length);
            for (var c = 0; c < channels; c++)
            {
                var submap = mux[c];
                _submapChannels[submap][submapSizes[submap]++] = c;
            }

            _mdct = mdct;
        }

        public void DecodePacket(IPacket packet, int blockSize, int channels, float[][] buffer)
        {
            var halfBlockSize = blockSize >> 1;

            // read the noise floor data
            var floorData = new IFloorData[_channelFloor.Length];
            var noExecuteChannel = new bool[_channelFloor.Length];
            for (var i = 0; i < _channelFloor.Length; i++)
            {
                floorData[i] = _channelFloor[i].Unpack(packet, blockSize, i);
                noExecuteChannel[i] = !floorData[i].ExecuteChannel;

                // pre-clear the residue buffers
                Array.Clear(buffer[i], 0, halfBlockSize);
            }

            // make sure we handle no-energy channels correctly given the couplings..
            for (var i = 0; i < _couplingAngle.Length; i++)
            {
                var angle = _couplingAngle[i];
                var magnitude = _couplingMangitude[i];
                if (!noExecuteChannel[angle] || !noExecuteChannel[magnitude])
                {
                    noExecuteChannel[angle] = false;
                    noExecuteChannel[magnitude] = false;
                }
            }

            // decode the submaps into the residue buffer
            for (var i = 0; i < _submapChannels.Length; i++)
            {
                var bundle = _submapChannels[i];
                if (bundle.Length == 0) continue;
                if (_submapChannels.Length == 1)
                {
                    _submapResidue[i].Decode(packet, noExecuteChannel, blockSize, buffer);
                    continue;
                }

                // Residue uses bundle-local channel indices, including for type 2 interleaving.
                var bundleSkip = new bool[bundle.Length];
                var bundleBuffer = new float[bundle.Length][];
                for (var j = 0; j < bundle.Length; j++)
                {
                    bundleSkip[j] = noExecuteChannel[bundle[j]];
                    bundleBuffer[j] = buffer[bundle[j]];
                }
                _submapResidue[i].Decode(packet, bundleSkip, blockSize, bundleBuffer);
            }

            // inverse coupling
            for (var i = _couplingAngle.Length - 1; i >= 0; i--)
            {
                if (!noExecuteChannel[_couplingAngle[i]] || !noExecuteChannel[_couplingMangitude[i]])
                {
                    var magnitude = buffer[_couplingMangitude[i]];
                    var angle = buffer[_couplingAngle[i]];

                    // we only have to do the first half; MDCT ignores the last half
                    for (var j = 0; j < halfBlockSize; j++)
                    {
                        float newM, newA;

                        var oldM = magnitude[j];
                        var oldA = angle[j];
                        if (oldM > 0)
                        {
                            if (oldA > 0)
                            {
                                newM = oldM;
                                newA = oldM - oldA;
                            }
                            else
                            {
                                newA = oldM;
                                newM = oldM + oldA;
                            }
                        }
                        else
                        {
                            if (oldA > 0)
                            {
                                newM = oldM;
                                newA = oldM + oldA;
                            }
                            else
                            {
                                newA = oldM;
                                newM = oldM - oldA;
                            }
                        }

                        magnitude[j] = newM;
                        angle[j] = newA;
                    }
                }
            }

            // apply floor / dot product / MDCT (only run if we have sound energy in that channel)
            for (var c = 0; c < _channelFloor.Length; c++)
            {
                if (floorData[c].ExecuteChannel)
                {
                    _channelFloor[c].Apply(floorData[c], blockSize, buffer[c]);
                    _mdct.Reverse(buffer[c], blockSize);
                }
                else
                {
                    // Coupling can decode residue for channels with no floor of their own.
                    Array.Clear(buffer[c], 0, blockSize);
                }
            }
        }
    }
}
