using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MapsetParser.objects.hitobjects
{
    public class Stackable : HitObject
    {
        public int stackIndex;

        public Vector2 UnstackedPosition { get => base.Position; }
        public override Vector2 Position { get => Offset(base.Position); }

        public Stackable(string aCode, Beatmap aBeatmap)
            : base(aCode, aBeatmap)
        {

        }

        private Vector2 Offset(Vector2 aPosition)
        {
            return new Vector2(Offset(aPosition.X), Offset(aPosition.Y));
        }

        private float Offset(float aValue)
        {
            return aValue * beatmap.difficultySettings.GetCircleRadius() * -0.1f;
        }
    }
}
