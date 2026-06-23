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

internal sealed class Electron2DAndroidDeviceSmokeObservation
{
    private Electron2DAndroidDeviceSmokeObservation(
        string status,
        string deviceSerial,
        string reason,
        IReadOnlyDictionary<string, bool> criteria)
    {
        Status = status;
        DeviceSerial = deviceSerial;
        Reason = reason;
        Criteria = criteria.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
    }

    public string Status { get; }

    public string DeviceSerial { get; }

    public string Reason { get; }

    public IReadOnlyDictionary<string, bool> Criteria { get; }

    public static Electron2DAndroidDeviceSmokeObservation Blocked(string reason)
    {
        return new Electron2DAndroidDeviceSmokeObservation(
            "blocked",
            string.Empty,
            reason,
            Electron2DAndroidDeviceSmokeRunner.RequiredCriteria.ToDictionary(criteria => criteria, _ => false, StringComparer.Ordinal));
    }

    public static Electron2DAndroidDeviceSmokeObservation Passed(string deviceSerial)
    {
        return new Electron2DAndroidDeviceSmokeObservation(
            "passed",
            deviceSerial,
            string.Empty,
            Electron2DAndroidDeviceSmokeRunner.RequiredCriteria.ToDictionary(criteria => criteria, _ => true, StringComparer.Ordinal));
    }
}
