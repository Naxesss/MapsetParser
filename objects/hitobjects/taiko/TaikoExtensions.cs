namespace MapsetParser.objects.hitobjects.taiko
{
    public static class TaikoExtensions
    {
        public static bool IsDon(this Circle circle)
        {
            return circle.hitSound == HitObject.HitSound.Clap || circle.hitSound == HitObject.HitSound.Whistle;
        }

        public static bool IsFinisher(this HitObject hitObject)
        {
            return hitObject.HasHitSound(HitObject.HitSound.Finish);
        }
    }
}
