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

        public Stackable(string aCode, Beatmap aBeatmap)
            : base(aCode, aBeatmap)
        {

        }

        /// <summary> Returns the same position but offseted to account for its stacking, if stacked. </summary>
        public Vector2 GetStackOffset(Vector2 aPosition) =>
            new Vector2(GetStackOffset(aPosition.X), GetStackOffset(aPosition.Y));

        private float GetStackOffset(float aValue) =>
            aValue + stackIndex * (beatmap?.difficultySettings.GetCircleRadius() ?? 0) * -0.1f;
    }
}
