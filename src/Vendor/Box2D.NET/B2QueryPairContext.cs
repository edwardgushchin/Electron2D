#nullable disable
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal struct B2QueryPairContext
    {
        public B2World world;
        public B2MoveResult moveResult;
        public B2BodyType queryTreeType;
        public int queryProxyKey;
        public int queryShapeIndex;
        internal bool includeBoundaries;
    }
}
