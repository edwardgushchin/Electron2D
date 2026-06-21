/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

internal readonly struct SdlGpuDeviceInfo
{
    public static readonly SdlGpuDeviceInfo Unknown = new(
        gpuName: "unknown",
        driverName: "unknown",
        driverVersion: "unknown",
        driverInfo: "unknown");

    public SdlGpuDeviceInfo(
        string gpuName,
        string driverName,
        string driverVersion,
        string driverInfo)
    {
        GpuName = Normalize(gpuName);
        DriverName = Normalize(driverName);
        DriverVersion = Normalize(driverVersion);
        DriverInfo = Normalize(driverInfo);
    }

    public string GpuName { get; }

    public string DriverName { get; }

    public string DriverVersion { get; }

    public string DriverInfo { get; }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
    }
}
