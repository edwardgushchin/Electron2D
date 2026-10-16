#nullable disable
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal class B2Sensor
    {
        // The owning world reuses retired sensors and releases their arrays on destruction.
        public B2Array<B2Visitor> hits;
        public B2Array<B2Visitor> overlaps1;
        public B2Array<B2Visitor> overlaps2;
        public int shapeId;
    }
}