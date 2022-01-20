using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Linq;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;

namespace MapsetParser.objects.hitobjects
{
    public class Slider : Stackable
    {
        // 319,179,1392,6,0,L|389:160,2,62.5,2|0|0,0:0|0:0|0:0,0:0:0:0:
        // x, y, time, typeFlags, hitsound, (sliderPath, edgeAmount, pixelLength, hitsoundEdges, additionEdges,) extras
        
        private static double stepLength = 0.0005;
        
        /// <summary> Determines how slider nodes affect the resulting shape of the slider. </summary>
        public enum CurveType
        {
            Linear,
            Passthrough,
            Bezier,
            Catmull
        }

        public readonly CurveType     curveType;
        public readonly List<Vector2> nodePositions;
        public readonly int           edgeAmount;
        public readonly float         pixelLength;

        // Hit sounding
        public readonly HitSound          startHitSound;
        public readonly Beatmap.Sampleset startSampleset;
        public readonly Beatmap.Sampleset startAddition;

        public readonly HitSound          endHitSound;
        public readonly Beatmap.Sampleset endSampleset;
        public readonly Beatmap.Sampleset endAddition;

        public readonly List<HitSound>          reverseHitSounds;
        public readonly List<Beatmap.Sampleset> reverseSamplesets;
        public readonly List<Beatmap.Sampleset> reverseAdditions;
        
        // Non-explicit
        private List<Vector2> bezierPoints;
        private double? curveDuration;
        private double? sliderSpeed;
        private double? curveLength;

        public readonly List<Vector2> pathPxPositions;
        public readonly List<Vector2> redAnchorPositions;

        public readonly double       endTime;
        public readonly List<double> sliderTickTimes;

        public Vector2 UnstackedEndPosition { get; private set; }
        public Vector2 EndPosition => UnstackedEndPosition + Position - UnstackedPosition;

        public Vector2 LazyEndPosition { get; set; }
        public double  LazyTravelDistance { get; set; }
        public double  LazyTravelTime { get; set; }

        public Slider(string[] args, Beatmap beatmap)
            : base(args, beatmap)
        {
            curveType          = GetSliderType(args);
            nodePositions      = GetNodes(args).ToList();
            edgeAmount         = GetEdgeAmount(args);
            pixelLength        = GetPixelLength(args);

            // Hit sounding
            var edgeHitSounds = GetEdgeHitSounds(args);
            var edgeAdditions = GetEdgeAdditions(args);

            startHitSound      = edgeHitSounds.Item1;
            startSampleset     = edgeAdditions.Item1;
            startAddition      = edgeAdditions.Item2;

            endHitSound        = edgeHitSounds.Item2;
            endSampleset       = edgeAdditions.Item3;
            endAddition        = edgeAdditions.Item4;

            reverseHitSounds    = edgeHitSounds.Item3.ToList();
            reverseSamplesets   = edgeAdditions.Item5.ToList();
            reverseAdditions    = edgeAdditions.Item6.ToList();

            // Non-explicit
            if (beatmap != null)
            {
                redAnchorPositions = GetRedAnchors().ToList();
                pathPxPositions = GetPathPxPositions();
                endTime = GetEndTime();
                sliderTickTimes = GetSliderTickTimes();

                UnstackedEndPosition = edgeAmount % 2 == 1 ? pathPxPositions.Last() : UnstackedPosition;

                // Difficulty
                LazyEndPosition = Position;
                LazyTravelDistance = 0;
                LazyTravelTime = 0;
            }

            usedHitSamples = GetUsedHitSamples().ToList();
        }

        /*
         *  Parsing
         */

        private CurveType GetSliderType(string[] args)
        {
            string type = args[5].Split('|')[0];
            return
                type == "L" ? CurveType.Linear :
                type == "P" ? CurveType.Passthrough :
                type == "B" ? CurveType.Bezier :
                CurveType.Catmull;  // Catmull is the default curve type.
        }
        
        private IEnumerable<Vector2> GetNodes(string[] args)
        {
            // The first position is also a node in the editor, so we count that too.
            yield return Position;

            string sliderPath = args[5];
            foreach(string node in sliderPath.Split('|'))
            {
                // Parses node format (e.g. P|128:50|172:291).
                if(node.Length > 1)
                {
                    float x = float.Parse(node.Split(':')[0]);
                    float y = float.Parse(node.Split(':')[1]);

                    yield return new Vector2(x, y);
                }
            }
        }
        
        private int GetEdgeAmount(string[] args) =>
            int.Parse(args[6]);
        
        private float GetPixelLength(string[] args) =>
            float.Parse(args[7], CultureInfo.InvariantCulture);
        
        private Tuple<HitSound, HitSound, List<HitSound>> GetEdgeHitSounds(string[] args)
        {
            HitSound edgeStartHitSound = 0;
            HitSound edgeEndHitSound = 0;
            List<HitSound> edgeReverseHitSounds = new List<HitSound>();

            if (args.Count() > 8)
            {
                // Not set in some situations (e.g. older file versions or no hit sounds).
                string edgeHsStr = args[8];
                if (edgeHsStr.Contains("|"))
                {
                    for (int i = 0; i < edgeHsStr.Split('|').Length; ++i)
                    {
                        HitSound hitSound = (HitSound)int.Parse(edgeHsStr.Split('|')[i]);

                        if (i == 0)
                            edgeStartHitSound = hitSound;
                        else if (i == edgeHsStr.Split('|').Length - 1)
                            edgeEndHitSound = hitSound;
                        else
                            // Any not first or last are for the reverses.
                            edgeReverseHitSounds.Add(hitSound);
                    }
                }
            }
            else
            {
                // If an object has no complex hit sounding, it omits fields such as edge
                // hit sounds. Instead, it simply uses one hit sound over everything.
                edgeStartHitSound = hitSound;
                edgeEndHitSound = hitSound;
                for (int i = 0; i < edgeAmount; ++i)
                    edgeReverseHitSounds.Add(hitSound);
            }

            return Tuple.Create(edgeStartHitSound, edgeEndHitSound, edgeReverseHitSounds);
        }
        
        private Tuple<Beatmap.Sampleset, Beatmap.Sampleset, Beatmap.Sampleset, Beatmap.Sampleset,
            List<Beatmap.Sampleset>, List<Beatmap.Sampleset>> GetEdgeAdditions(string[] args)
        {
            Beatmap.Sampleset edgeStartSampleset = 0;
            Beatmap.Sampleset edgeStartAddition  = 0;

            Beatmap.Sampleset edgeEndSampleset = 0;
            Beatmap.Sampleset edgeEndAddition  = 0;

            List<Beatmap.Sampleset> edgeReverseSamplesets = new List<Beatmap.Sampleset>();
            List<Beatmap.Sampleset> edgeReverseAdditions  = new List<Beatmap.Sampleset>();

            if (args.Count() > 9)
            {
                // Not set in some situations (e.g. older file versions or no hit sounds).
                string edgeAdditions = args[9];
                if (edgeAdditions.Contains("|"))
                {
                    for (int i = 0; i < edgeAdditions.Split('|').Length; ++i)
                    {
                        Beatmap.Sampleset sampleset = (Beatmap.Sampleset)int.Parse(edgeAdditions.Split('|')[i].Split(':')[0]);
                        Beatmap.Sampleset addition  = (Beatmap.Sampleset)int.Parse(edgeAdditions.Split('|')[i].Split(':')[1]);

                        if (i == 0)
                        {
                            edgeStartSampleset = sampleset;
                            edgeStartAddition  = addition;
                        }
                        else if (i == edgeAdditions.Split('|').Length - 1)
                        {
                            edgeEndSampleset = sampleset;
                            edgeEndAddition  = addition;
                        }
                        else
                        {
                            edgeReverseSamplesets.Add(sampleset);
                            edgeReverseAdditions.Add(addition);
                        }
                    }
                }
            }
            else
            {
                // If an object has no complex hit sounding, it omits fields such as edge
                // hit sounds. Instead, it simply uses one hit sound over everything.
                edgeStartSampleset = sampleset;
                edgeEndSampleset = sampleset;
                for (int i = 0; i < edgeAmount; ++i)
                    edgeReverseSamplesets.Add(sampleset);

                edgeStartAddition = addition;
                edgeEndAddition = addition;
                for (int i = 0; i < edgeAmount; ++i)
                    edgeReverseAdditions.Add(addition);
            }

            return Tuple.Create(edgeStartSampleset, edgeStartAddition, edgeEndSampleset, edgeEndAddition, edgeReverseSamplesets, edgeReverseAdditions);
        }

        /*
         *  Non-Explicit
         */
        
        private new double GetEndTime()
        {
            double start = time;
            double curveDuration = GetCurveDuration();
            double exactEndTime = start + curveDuration * edgeAmount;

            return exactEndTime + beatmap.GetPracticalUnsnap(exactEndTime);
        }

        private IEnumerable<Vector2> GetRedAnchors()
        {
            if (nodePositions.Count > 0)
            {
                Vector2 prevPosition = nodePositions[0];
                for (int i = 1; i < nodePositions.Count; ++i)
                {
                    if (nodePositions[i] == prevPosition)
                        yield return nodePositions[i];
                    prevPosition = nodePositions[i];
                }
            }
        }

        private List<Vector2> GetPathPxPositions()
        {
            // Increase this to improve performance but lower accuracy.
            double multiplier = 1;

            // First we need to get how fast the slider moves,
            double pxPerMs = GetSliderSpeed(time);

            // and then calculate this in steps accordingly.
            Vector2 prevPosition;
            Vector2 currentPosition = UnstackedPosition;

            // Always start with the current position, means reverse sliders' end position is more accurate.
            List<Vector2> positions = new List<Vector2>() { currentPosition };

            double limit = pxPerMs * GetCurveDuration() / multiplier;
            
            for (int i = 0; i < limit; ++i)
            {
                prevPosition = currentPosition;
                double time = base.time + i / pxPerMs * multiplier;
                
                currentPosition = GetPathPosition(time);

                // Only add the position if it's different from the previous.
                if (currentPosition != prevPosition)
                    positions.Add(currentPosition);
            }

            Vector2 endPosition = GetPathPosition(time + limit / pxPerMs * multiplier);
            positions.Add(endPosition);

            return positions;
        }

        /*
         *  Utility
         */

        /// <summary> Returns the position on the curve at a given point in time (intensive, consider using mPathPxPositions). </summary>
        public Vector2 GetPathPosition(double time)
        {
            return curveType switch
            {
                CurveType.Linear => GetLinearPathPosition(time),
                CurveType.Passthrough => GetPassthroughPathPosition(time),
                CurveType.Bezier => GetBezierPathPosition(time),
                CurveType.Catmull => GetCatmullPathPosition(time),
                _ => new Vector2(0, 0),
            };
        }

        /// <summary> Returns the speed of any slider starting from the given time in px/ms. Caps SV within range 0.1-10. </summary>
        public double GetSliderSpeed(double time)
        {
            if (sliderSpeed != null)
                return sliderSpeed.GetValueOrDefault();

            double msPerBeat          = beatmap.GetTimingLine<UninheritedLine>(time).msPerBeat;
            double effectiveSVMult    = beatmap.GetTimingLine(base.time).svMult;

            sliderSpeed = 100 * effectiveSVMult * beatmap.difficultySettings.sliderMultiplier / msPerBeat;
            return sliderSpeed.GetValueOrDefault();
        }

        /// <summary> Returns the duration of the curve (i.e. from edge to edge), ignoring reverses. </summary>
        public double GetCurveDuration()
        {
            if (curveDuration != null)
                return curveDuration.GetValueOrDefault();

            curveDuration = pixelLength / GetSliderSpeed(time);
            return curveDuration.GetValueOrDefault();
        }

        /// <summary> Returns the sampleset on the head of the slider, optionally prioritizing the addition. </summary>
        public new Beatmap.Sampleset GetStartSampleset(bool additionOverrides = false)
        {
            if (additionOverrides && startAddition != Beatmap.Sampleset.Auto)
                return startAddition;

            // Inherits from timing line if auto.
            return startSampleset == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(time, true).sampleset : startSampleset;
        }

        /// <summary> Returns the sampleset at a given reverse (starting from 0), optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset GetReverseSampleset(int reverseIndex, bool additionOverrides = false)
        {
            double theoreticalStart = base.time - beatmap.GetTheoreticalUnsnap(base.time);
            double time = Timestamp.Round(theoreticalStart + GetCurveDuration() * (reverseIndex + 1));

            // Reverse additions and samplesets do not exist in file version 7 and below, hence ElementAtOrDefault.
            if (additionOverrides && reverseAdditions.ElementAtOrDefault(reverseIndex) != Beatmap.Sampleset.Auto)
                return reverseAdditions.ElementAt(reverseIndex);

            return reverseSamplesets.ElementAtOrDefault(reverseIndex) == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(time, true).sampleset : reverseSamplesets.ElementAt(reverseIndex);
        }

        /// <summary> Returns the sampleset on the tail of the slider, optionally prioritizing the addition. </summary>
        public new Beatmap.Sampleset GetEndSampleset(bool additionOverrides = false)
        {
            if (additionOverrides && endAddition != Beatmap.Sampleset.Auto)
                return endAddition;

            return endSampleset == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(endTime, true).sampleset : endSampleset;
        }

        /// <summary> Returns how far along the curve a given point of time is (from 0 to 1), accounting for reverses. </summary>
        public double GetCurveFraction(double time)
        {
            double division = (time - base.time) / GetCurveDuration();
            double fraction = division - Math.Floor(division);
            
            if (Math.Floor(division) % 2 == 1)
                fraction = 1 - fraction;

            return fraction;
        }

        /// <summary> Returns the length of the curve in px. </summary>
        public double GetCurveLength()
        {
            if (curveLength != null)
                return curveLength.GetValueOrDefault();

            curveLength = GetCurveDuration() * GetSliderSpeed(time);
            return curveLength.GetValueOrDefault();
        }

        /// <summary> Returns the points in time for all ticks of the slider, with decimal accuracy. </summary>
        public List<double> GetSliderTickTimes()
        {
            float tickRate = beatmap.difficultySettings.sliderTickRate;
            double msPerBeat = beatmap.GetTimingLine<UninheritedLine>(time).msPerBeat;

            // Not entierly sure if it's based on theoretical time and cast to int or something else.
            // It doesn't seem to be practical time and then rounded to closest at least.
            double theoreticalTime = time - beatmap.GetTheoreticalUnsnap(time);

            // Only duration during which ticks can be present (so not the same ms as the tail).
            double duration = endTime - time - 1;
            int ticks = (int)(duration / msPerBeat * tickRate);
            List<double> tickTimes = new List<double>();
            for (int i = 1; i <= ticks; ++i)
                tickTimes.Add(Timestamp.Round(i * msPerBeat / tickRate + theoreticalTime));

            return tickTimes;
        }
        
        /*
         *  Mathematics
         */

        private Vector2 GetBezierPoint(List<Vector2> points, double fraction)
        {
            // See https://en.wikipedia.org/wiki/B%C3%A9zier_curve.
            // Finds the middle of middles at x, which is a variable between 0 and 1.
            // Note that this is not a constant movement, though.

            // Make sure to copy, don't reference; newPoints will be mutated.
            List<Vector2> newPoints = new List<Vector2>(points);

            int index = newPoints.Count - 1;
            while (index > 0)
            {
                for (int i = 0; i < index; i++)
                    newPoints[i] = newPoints[i] + (float)fraction * (newPoints[i + 1] - newPoints[i]);
                index--;
            }
            return newPoints[0];
        }

        private Vector2 GetCatmullPoint(Vector2 point0, Vector2 point1, Vector2 point2, Vector2 point3, double x)
        {
            // See https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline.

            Vector2 point = new Vector2();

            float x2 = (float)(x * x);
            float x3 = x2 * (float)x;

            point.X = 0.5f * ((2.0f * point1.X) + (-point0.X + point2.X) * (float)x +
                (2.0f * point0.X - 5.0f * point1.X + 4 * point2.X - point3.X) * x2 +
                (-point0.X + 3.0f * point1.X - 3.0f * point2.X + point3.X) * x3);

            point.Y = 0.5f * ((2.0f * point1.Y) + (-point0.Y + point2.Y) * (float)x +
                (2.0f * point0.Y - 5.0f * point1.Y + 4 * point2.Y - point3.Y) * x2 +
                (-point0.Y + 3.0f * point1.Y - 3.0f * point2.Y + point3.Y) * x3);

            return point;
        }

        private double GetDistance(Vector2 position, Vector2 otherPosition) =>
            Math.Sqrt(
                Math.Pow(position.X - otherPosition.X, 2) +
                Math.Pow(position.Y - otherPosition.Y, 2));

        /*
         *  Slider Pathing
         */
        
        private Vector2 GetLinearPathPosition(double time)
        {
            double fraction = GetCurveFraction(time);
            
            List<double> pathLengths = new List<double>();
            Vector2 previousPosition = Position;
            for(int i = 1; i < nodePositions.Count; ++i)
            {
                // Since every node is interpreted as an anchor, we only need to worry about the last node.
                // Rest will be perfectly followed by just going straight to the node,
                double distance;
                if (i < nodePositions.Count - 1)
                {
                    distance          = GetDistance(nodePositions.ElementAt(i), previousPosition);
                    previousPosition  = nodePositions.ElementAt(i);
                }
                else
                    // but if it is the last node, then we need to look at the total length
                    // to see how far it goes in that direction.
                    distance = GetCurveLength() - pathLengths.Sum();

                pathLengths.Add(distance);
            }
            
            double fractionDistance = pathLengths.Sum() * fraction;
            int prevNodeIndex = 0;
            foreach(double pathLength in pathLengths)
            {
                ++prevNodeIndex;

                if (fractionDistance > pathLength)
                    fractionDistance -= pathLength;
                else
                    break;
            }

            if (prevNodeIndex >= nodePositions.Count())
                prevNodeIndex = nodePositions.Count() - 1;

            Vector2 startPoint    = nodePositions.ElementAt(prevNodeIndex <= 0 ? 0 : prevNodeIndex - 1);
            Vector2 endPoint      = nodePositions.ElementAt(prevNodeIndex);
            double pointDistance  = GetDistance(startPoint, endPoint);
            double microFraction  = fractionDistance / pointDistance;

            return startPoint + new Vector2(
                (endPoint - startPoint).X * (float)microFraction,
                (endPoint - startPoint).Y * (float)microFraction
            );
        }

        private Vector2 GetPassthroughPathPosition(double time)
        {
            // Less than 3 interprets as linear.
            if (nodePositions.Count < 3)
                return GetLinearPathPosition(time);

            // More than 3 interprets as bezier.
            if (nodePositions.Count > 3)
                return GetBezierPathPosition(time);
            
            Vector2 secondPoint   = nodePositions.ElementAt(1);
            Vector2 thirdPoint    = nodePositions.ElementAt(2);

            // Center and radius of the circle.
            double divisor = 2 * (UnstackedPosition.X * (secondPoint.Y - thirdPoint.Y) + secondPoint.X *
                (thirdPoint.Y - UnstackedPosition.Y) + thirdPoint.X * (UnstackedPosition.Y - secondPoint.Y));

            if (divisor == 0)
                // Second point is somewhere straight between the first and third, making our path linear.
                return GetLinearPathPosition(time);

            double centerX = ((UnstackedPosition.X * UnstackedPosition.X + UnstackedPosition.Y * UnstackedPosition.Y) *
                (secondPoint.Y - thirdPoint.Y) + (secondPoint.X * secondPoint.X + secondPoint.Y * secondPoint.Y) *
                (thirdPoint.Y - UnstackedPosition.Y) + (thirdPoint.X * thirdPoint.X + thirdPoint.Y * thirdPoint.Y) *
                (UnstackedPosition.Y - secondPoint.Y)) / divisor;
            double centerY = ((UnstackedPosition.X * UnstackedPosition.X + UnstackedPosition.Y * UnstackedPosition.Y) *
                (thirdPoint.X - secondPoint.X) + (secondPoint.X * secondPoint.X + secondPoint.Y * secondPoint.Y) *
                (UnstackedPosition.X - thirdPoint.X) + (thirdPoint.X * thirdPoint.X + thirdPoint.Y * thirdPoint.Y) *
                (secondPoint.X - UnstackedPosition.X)) / divisor;

            double radius = Math.Sqrt(Math.Pow((centerX - UnstackedPosition.X), 2) + Math.Pow((centerY - UnstackedPosition.Y), 2));

            double radians = GetCurveLength() / radius;

            // Which direction to rotate based on which side the center is on.
            if (((secondPoint.X - UnstackedPosition.X) * (thirdPoint.Y - UnstackedPosition.Y) - (secondPoint.Y - UnstackedPosition.Y) * (thirdPoint.X - UnstackedPosition.X)) < 0)
                radians *= -1.0f;
            
            // Getting the point on the circumference of the circle.
            double fraction   = GetCurveFraction(time);

            double radianX = Math.Cos(fraction * radians);
            double radianY = Math.Sin(fraction * radians);

            double x = (radianX * (UnstackedPosition.X - centerX)) - (radianY * (UnstackedPosition.Y - centerY)) + centerX;
            double y = (radianY * (UnstackedPosition.X - centerX)) + (radianX * (UnstackedPosition.Y - centerY)) + centerY;

            return new Vector2((float)x, (float)y);
        }

        private List<Vector2> GetBezierPoints()
        {
            // Include the first point in the total slider points.
            List<Vector2> sliderPoints = nodePositions.ToList();

            Vector2 currentPoint = Position;
            List<Vector2> tempBezierPoints = new List<Vector2>() { currentPoint };

            // For each anchor, calculate the curve, until we find where we need to be.
            int tteration = 0;

            double pixelsPerMs = GetSliderSpeed(time);
            double totalLength = 0;
            double fullLength = GetCurveDuration() * pixelsPerMs;
            
            while (tteration < sliderPoints.Count)
            {
                // Get all the nodes from one anchor/start point to the next.
                List<Vector2> points = new List<Vector2>();
                int currentIteration = tteration;
                for (int i = currentIteration; i < sliderPoints.Count; ++i)
                {
                    if (i > currentIteration && sliderPoints.ElementAt(i - 1) == sliderPoints.ElementAt(i))
                        break;
                    points.Add(sliderPoints.ElementAt(i));
                    ++tteration;
                }

                // Calculate how long this curve (not the whole thing, just from anchor to anchor) will be.
                Vector2 prevPoint = points.First();
                double curvePixelLength = 0;
                for (double k = 0.0f; k < 1.0f + stepLength; k += stepLength)
                {
                    if (totalLength <= fullLength)
                    {
                        currentPoint = GetBezierPoint(points, k);
                        curvePixelLength += GetDistance(prevPoint, currentPoint);
                        prevPoint = currentPoint;

                        if (curvePixelLength >= pixelsPerMs * 2)
                        {
                            totalLength += curvePixelLength;
                            curvePixelLength = 0;
                            tempBezierPoints.Add(currentPoint);
                        }
                    }
                }

                // As long as we haven't reached the last path between anchors, keep track of the length of the path.
                // Ensures that we can switch from one anchor path to another.
                if (tteration <= sliderPoints.Count)
                    totalLength += curvePixelLength;
                else
                    tempBezierPoints.Add(currentPoint);
            }

            return tempBezierPoints;
        }

        private Vector2 GetBezierPathPosition(double time)
        {
            if (bezierPoints == null)
                bezierPoints = GetBezierPoints();

            double fraction = GetCurveFraction(time);

            int     integer = (int)Math.Floor(bezierPoints.Count * fraction);
            float   @decimal = (float)(bezierPoints.Count * fraction - integer);
            
            return integer >= bezierPoints.Count - 1
                    ? bezierPoints[bezierPoints.Count - 1]
                    : bezierPoints[integer] + (bezierPoints[integer + 1] - bezierPoints[integer]) * @decimal;
        }

        private Vector2 GetCatmullPathPosition(double time)
        {
            // Any less than 3 points might as well be linear.
            if (nodePositions.Count < 3)
                return GetLinearPathPosition(time);

            double fraction = GetCurveFraction(time);

            double pixelsPerMs = GetSliderSpeed(base.time);
            double totalLength = 0;
            double desiredLength = GetCurveDuration() * pixelsPerMs * fraction;
            
            List<Vector2> points = new List<Vector2>(nodePositions);
            
            // Go through the curve until the fraction is reached.
            Vector2 prevPoint = points.First();
            for (int i = 0; i < points.Count - 1; ++i)
            {
                // Get the curve length between anchors.
                double curvePixelLength = 0;
                Vector2 prevCurvePoint = points[i];
                for (double k = 0.0f; k < 1.0f + stepLength; k += stepLength)
                {
                    Vector2 currentPoint;
                    if (i == 0)
                        // Double the start position.
                        currentPoint = GetCatmullPoint(points[i], points[i], points[i + 1], points[i + 2], k);
                    else if (i < points.Count - 2)
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 2], k);
                    else
                        // Double the end position.
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 1], k);

                    curvePixelLength += Math.Sqrt(
                        Math.Pow(prevCurvePoint.X - currentPoint.X, 2) +
                        Math.Pow(prevCurvePoint.Y - currentPoint.Y, 2));
                    prevCurvePoint = currentPoint;
                }
                
                double variable = 0;
                double curveLength = 0;
                while (true)
                {
                    Vector2 currentPoint;
                    if (i == 0)
                        currentPoint = GetCatmullPoint(points[i], points[i], points[i + 1], points[i + 2], variable);
                    else if (i < points.Count - 2)
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 2], variable);
                    else
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 1], variable);

                    curveLength += Math.Sqrt(
                        Math.Pow(prevPoint.X - currentPoint.X, 2) +
                        Math.Pow(prevPoint.Y - currentPoint.Y, 2));
                    
                    if(totalLength + curveLength >= desiredLength)
                        return currentPoint;
                    prevPoint = currentPoint;
                    
                    // Keeping track of the length of the path ensures that we can switch from one anchor path to another.
                    if (curveLength > curvePixelLength
                        && i < points.Count - 2)
                    {
                        totalLength += curveLength;
                        break;
                    }

                    variable += stepLength;
                }
            }
            return new Vector2(0, 0);
        }
    }
}
