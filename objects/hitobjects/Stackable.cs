using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MapsetParser.objects.hitobjects
{
    public class Stackable : HitObject
    {
        public int stackIndex;
        public bool isOnSlider;

        public Vector2 UnstackedPosition { get => base.Position; }
        public override Vector2 Position { get => GetStackOffset(base.Position); }

        public Stackable(string[] args, Beatmap beatmap)
            : base(args, beatmap)
        {

        }

        /// <summary> Returns the same position but offseted to account for its stacking, if stacked. </summary>
        public Vector2 GetStackOffset(Vector2 position) =>
            new Vector2(GetStackOffset(position.X), GetStackOffset(position.Y));

        private float GetStackOffset(float value) =>
            value + stackIndex * (beatmap?.difficultySettings.GetCircleRadius() ?? 0) * -0.1f;
    }
}
