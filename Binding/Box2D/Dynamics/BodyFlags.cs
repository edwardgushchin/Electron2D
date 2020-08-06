using System;

namespace Electron2D.Binding.Box2D.Dynamics
{
    [Flags]
    #pragma warning disable RCS1135
    public enum BodyFlags
    {
        /// <summary>
        /// 孤岛
        /// </summary>
        Island = 1 << 0,

        /// <summary>
        /// 醒着的
        /// </summary>
        IsAwake = 1 << 1,

        /// <summary>
        /// 自动休眠
        /// </summary>
        AutoSleep = 1 << 2,

        /// <summary>
        /// 子弹
        /// </summary>
        IsBullet = 1 << 3,

        FixedRotation = 1 << 4,

        /// <summary>
        /// 活跃
        /// </summary>
        IsEnabled = 1 << 5,

        /// <summary>
        /// 碰撞时间
        /// </summary>
        Toi = 1 << 6
    }
}