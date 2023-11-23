// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MapsetParser.objects.taiko
{
    public class Hit : HitObject
    {
        public Hit(string[] args, Beatmap beatmap) : base(args, beatmap)
        {
            Type = GetType();
        }

        /// <summary>
        /// The <see cref="HitType"/> that actuates this <see cref="Hit"/>.
        /// </summary>
        public HitType Type;

        private HitType GetType()
        {
            switch (hitSound)
            {
                case HitSound.Whistle:
                    return HitType.Rim;
                case HitSound.Clap:
                    return HitType.Rim;
                default:
                    return HitType.Centre;
            }
        }
    }
}
