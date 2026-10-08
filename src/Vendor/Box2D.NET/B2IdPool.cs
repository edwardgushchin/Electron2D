#nullable disable
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal class B2IdPool
    {
        public B2Array<int> freeArray;
        public int nextIndex;
        internal System.Action<int, bool> changed;

        public void Clear()
        {
            freeArray = new B2Array<int>();
            nextIndex = 0;
            changed = null;
        }
    }
}
