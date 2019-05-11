using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Linq;
using MapsetParser.objects.timinglines;

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
            Catmull,
            Unknown
        }

        public readonly CurveType     curveType;
        public readonly List<Vector2> nodePositions;
        public readonly int           edgeAmount;
        public readonly float         pixelLength;

        // hit sounding
        public readonly HitSound          startHitSound;
        public readonly Beatmap.Sampleset startSampleset;
        public readonly Beatmap.Sampleset startAddition;

        public readonly HitSound          endHitSound;
        public readonly Beatmap.Sampleset endSampleset;
        public readonly Beatmap.Sampleset endAddition;

        public readonly List<HitSound>          repeatHitSounds;
        public readonly List<Beatmap.Sampleset> repeatSamplesets;
        public readonly List<Beatmap.Sampleset> repeatAdditions;
        
        // non-explicit
        private List<Vector2> bezierPoints;
        private double? duration;

        public readonly List<Vector2> pathPxPositions;
        public readonly List<Vector2> redAnchorPositions;

        public readonly double       endTime;
        public readonly List<double> sliderTickTimes;

        public Vector2 UnstackedEndPosition { get; private set; }
        public Vector2 EndPosition { get; private set; }

        public Slider(string aCode, Beatmap aBeatmap)
            : base(aCode, aBeatmap)
        {
            curveType          = GetSliderType(aCode);
            nodePositions      = GetNodes(aCode).ToList();
            edgeAmount         = GetEdgeAmount(aCode);
            pixelLength        = GetPixelLength(aCode);

            // hit sounding
            var edgeHitSounds = GetEdgeHitSounds(aCode);
            var edgeAdditions = GetEdgeAdditions(aCode);

            startHitSound      = edgeHitSounds.Item1;
            startSampleset     = edgeAdditions.Item1;
            startAddition      = edgeAdditions.Item2;

            endHitSound        = edgeHitSounds.Item2;
            endSampleset       = edgeAdditions.Item3;
            endAddition        = edgeAdditions.Item4;

            repeatHitSounds    = edgeHitSounds.Item3.ToList();
            repeatSamplesets   = edgeAdditions.Item5.ToList();
            repeatAdditions    = edgeAdditions.Item6.ToList();

            // non-explicit
            if (beatmap != null)
            {
                redAnchorPositions = GetRedAnchors().ToList();
                pathPxPositions    = GetPathPxPositions();
                endTime            = GetEndTime();
                sliderTickTimes    = GetSliderTickTimes();

                UnstackedEndPosition = edgeAmount % 2 == 1 ? pathPxPositions.Last() : UnstackedPosition;
                EndPosition          = GetStackOffset(UnstackedEndPosition);
            }
        }

        /*
         *  Parsing
         */

        private CurveType GetSliderType(string aCode)
        {
            string type = aCode.Split(',')[5].Split('|')[0];
            return
                type == "L" ? CurveType.Linear :
                type == "P" ? CurveType.Passthrough :
                type == "B" ? CurveType.Bezier :
                type == "C" ? CurveType.Catmull :
                CurveType.Unknown;
        }
        
        private IEnumerable<Vector2> GetNodes(string aCode)
        {
            // the first position is also a node in the editor so we count that too
            yield return Position;

            string sliderPath = aCode.Split(',')[5];
            foreach(string node in sliderPath.Split('|'))
            {
                // ignores the slider type P|128:50|172:291
                if(node.Length > 1)
                {
                    float x = float.Parse(node.Split(':')[0]);
                    float y = float.Parse(node.Split(':')[1]);

                    yield return new Vector2(x, y);
                }
            }
        }
        
        private int GetEdgeAmount(string aCode)
        {
            return int.Parse(aCode.Split(',')[6]);
        }
        
        private float GetPixelLength(string aCode)
        {
            return float.Parse(aCode.Split(',')[7], CultureInfo.InvariantCulture);
        }
        
        private Tuple<HitSound, HitSound, IEnumerable<HitSound>> GetEdgeHitSounds(string aCode)
        {
            HitSound startHitSound = 0;
            HitSound endHitSound = 0;
            IEnumerable<HitSound> repeatHitSounds = new List<HitSound>();

            if (aCode.Split(',').Count() > 8)
            {
                string edgeHitSounds = aCode.Split(',')[8];

                // not set in some situations
                if (edgeHitSounds.Contains("|"))
                {
                    for (int i = 0; i < edgeHitSounds.Split('|').Length; ++i)
                    {
                        HitSound hitSound = (HitSound)int.Parse(edgeHitSounds.Split('|')[i]);

                        // first is start
                        if (i == 0)
                            startHitSound = hitSound;
                        // last is end
                        else if (i == edgeHitSounds.Split('|').Length - 1)
                            endHitSound = hitSound;
                        // all the others are repeats
                        else
                            repeatHitSounds = repeatHitSounds.Concat(new HitSound[] { hitSound });
                    }
                }
            }

            return Tuple.Create(startHitSound, endHitSound, repeatHitSounds);
        }
        
        private Tuple<Beatmap.Sampleset, Beatmap.Sampleset, Beatmap.Sampleset, Beatmap.Sampleset,
            IEnumerable<Beatmap.Sampleset>, IEnumerable<Beatmap.Sampleset>> GetEdgeAdditions(string aCode)
        {
            Beatmap.Sampleset startSampleset = 0;
            Beatmap.Sampleset startAddition  = 0;

            Beatmap.Sampleset endSampleset = 0;
            Beatmap.Sampleset endAddition  = 0;

            IEnumerable<Beatmap.Sampleset> repeatSamplesets = new List<Beatmap.Sampleset>();
            IEnumerable<Beatmap.Sampleset> repeatAdditions  = new List<Beatmap.Sampleset>();

            if (aCode.Split(',').Count() > 9)
            {
                string edgeAdditions = aCode.Split(',')[9];

                // not set in some situations
                if (edgeAdditions.Contains("|"))
                {
                    for (int i = 0; i < edgeAdditions.Split('|').Length; ++i)
                    {
                        Beatmap.Sampleset sampleset = (Beatmap.Sampleset)int.Parse(edgeAdditions.Split('|')[i].Split(':')[0]);
                        Beatmap.Sampleset addition  = (Beatmap.Sampleset)int.Parse(edgeAdditions.Split('|')[i].Split(':')[1]);

                        // first is start
                        if (i == 0)
                        {
                            startSampleset = sampleset;
                            startAddition  = addition;
                        }
                        // last is end
                        else if (i == edgeAdditions.Split('|').Length - 1)
                        {
                            endSampleset = sampleset;
                            endAddition  = addition;
                        }
                        // all the others are repeats
                        else
                        {
                            repeatSamplesets = repeatSamplesets.Concat(new Beatmap.Sampleset[] { sampleset });
                            repeatAdditions  = repeatAdditions .Concat(new Beatmap.Sampleset[] { sampleset });
                        }
                    }
                }
            }

            return Tuple.Create(startSampleset, startAddition, endSampleset, endAddition, repeatSamplesets, repeatAdditions);
        }

        /*
         *  Non-Explicit
         */
        
        private new double GetEndTime()
        {
            double start = time;
            double duration = GetCurveDuration();
            double endTime = start + duration * edgeAmount;

            return endTime + beatmap.GetPracticalUnsnap(endTime);
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
            // increase this to improve performance but lower accuracy
            double multiplier = 1;

            // first we need to get how fast the slider moves
            double pxPerMs = GetSliderSpeed(time);

            // and then calculate this in steps accordingly
            Vector2 prevPosition;
            Vector2 currentPosition = UnstackedPosition;

            // always start with the current position, means reverse sliders' end position is more accurate
            List<Vector2> positions = new List<Vector2>() { currentPosition };

            double limit = pxPerMs * GetCurveDuration() / multiplier;
            
            for (int i = 0; i < limit; ++i)
            {
                prevPosition = currentPosition;
                double time = base.time + i / pxPerMs * multiplier;
                
                currentPosition = GetPathPosition(time);

                // only add the position if it's different from the previous
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
        public Vector2 GetPathPosition(double aTime)
        {
            switch (curveType)
            {
                case CurveType.Linear:
                    return GetLinearPathPosition(aTime);
                case CurveType.Passthrough:
                    return GetPassthroughPathPosition(aTime);
                case CurveType.Bezier:
                    return GetBezierPathPosition(aTime);
                case CurveType.Catmull:
                    return GetCatmullPathPosition(aTime);
                default:
                    return new Vector2(0, 0);
            }
        }

        /// <summary> Returns the speed of any slider starting from the given time in px/ms. </summary>
        public double GetSliderSpeed(double aTime)
        {
            // the game acts as if anything less is equal to this
            double minSVMult = 0.1;

            double msPerBeat          = (beatmap.GetTimingLine(aTime, true) as UninheritedLine).msPerBeat;
            double effectiveSVMult    = beatmap.GetTimingLine(time).svMult < minSVMult ? minSVMult : beatmap.GetTimingLine(time).svMult;
            double sliderSpeed        = 100 * effectiveSVMult * beatmap.difficultySettings.sliderMultiplier / msPerBeat;

            return sliderSpeed;
        }

        /// <summary> Returns the duration of the curve (i.e. from edge to edge), ignoring repeats. </summary>
        public double GetCurveDuration()
        {
            if (duration != null)
                return duration.GetValueOrDefault();
            
            double sliderSpeed    = GetSliderSpeed(time);
            double result         = pixelLength / sliderSpeed;

            duration = result;
            return result;
        }

        /// <summary> Returns the sampleset on the head of the slider, optionally prioritizing the addition. </summary>
        public new Beatmap.Sampleset GetStartSampleset(bool anAddition = false)
        {
            if (anAddition && startAddition != Beatmap.Sampleset.Auto)
                return startAddition;

            // inherits from timing line if auto
            return startSampleset == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(time, false, true).sampleset : startSampleset;
        }

        /// <summary> Returns the sampleset at a given repeat (starting from 0), optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset GetRepeatSampleset(int aRepeatIndex, bool anAddition = false)
        {
            double theoreticalStart = base.time - beatmap.GetTheoreticalUnsnap(base.time);
            double time = Math.Floor(theoreticalStart + GetCurveDuration() * (aRepeatIndex + 1));

            if (anAddition && repeatAdditions.ElementAt(aRepeatIndex) != Beatmap.Sampleset.Auto)
                return repeatAdditions.ElementAt(aRepeatIndex);

            // doesn't exist in file version 9
            return repeatSamplesets.Count() == 0 || repeatSamplesets.ElementAt(aRepeatIndex) == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(time, false, true).sampleset : repeatSamplesets.ElementAt(aRepeatIndex);
        }

        /// <summary> Returns the sampleset on the tail of the slider, optionally prioritizing the addition. </summary>
        public new Beatmap.Sampleset GetEndSampleset(bool anAddition = false)
        {
            if (anAddition && endAddition != Beatmap.Sampleset.Auto)
                return endAddition;

            return endSampleset == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(endTime, false, true).sampleset : endSampleset;
        }

        /// <summary> Returns how far along the curve a given point of time is (from 0 to 1), accounting for repeats. </summary>
        public double GetCurveFraction(double aTime)
        {
            double division = (aTime - time) / GetCurveDuration();
            double fraction = division - Math.Floor(division);
            
            if (Math.Floor(division) % 2 == 1)
                fraction = 1 - fraction;

            return fraction;
        }

        /// <summary> Returns the length of the curve in px. </summary>
        public double GetCurveLength()
        {
            double totalTime      = GetCurveDuration();
            double pixelsPerMs    = GetSliderSpeed(time);
            double length         = totalTime * pixelsPerMs;

            return length;
        }

        /// <summary> Returns the points in time for all ticks of the slider, with decimal accuracy. </summary>
        public List<double> GetSliderTickTimes()
        {
            float tickRate = beatmap.difficultySettings.sliderTickRate;
            double msPerBeat = (beatmap.GetTimingLine(time, true) as UninheritedLine).msPerBeat;

            double theoreticalTime = time - beatmap.GetTheoreticalUnsnap(time);
            
            List<double> times = new List<double>();
            for(int i = 1; i < Math.Floor(GetCurveDuration() / msPerBeat * tickRate); ++i)
                times.Add(Math.Floor(i * msPerBeat / tickRate + theoreticalTime));

            return times;
        }
        
        /*
         *  Mathematics
         */

        private Vector2 GetBezierPoint(List<Vector2> aPoints, double aFraction)
        {
            // does the whole bezier magic thing with the connecting lines and all that
            // finds the middle of middles at aX, which is a variable between 0 and 1
            // take note that this is not a constant movement though

            // make sure to copy, don't reference
            List<Vector2> newPoints = new List<Vector2>(aPoints);

            int index = newPoints.Count - 1;
            while (index > 0)
            {
                for (int i = 0; i < index; i++)
                    newPoints[i] = newPoints[i] + (float)aFraction * (newPoints[i + 1] - newPoints[i]);
                index--;
            }
            return newPoints[0];
        }

        private Vector2 GetCatmullPoint(Vector2 aPoint0, Vector2 aPoint1, Vector2 aPoint2, Vector2 aPoint3, double aX)
        {
            Vector2 point = new Vector2();

            float x2 = (float)(aX * aX);
            float x3 = x2 * (float)aX;

            point.X = 0.5f * ((2.0f * aPoint1.X) + (-aPoint0.X + aPoint2.X) * (float)aX +
                (2.0f * aPoint0.X - 5.0f * aPoint1.X + 4 * aPoint2.X - aPoint3.X) * x2 +
                (-aPoint0.X + 3.0f * aPoint1.X - 3.0f * aPoint2.X + aPoint3.X) * x3);

            point.Y = 0.5f * ((2.0f * aPoint1.Y) + (-aPoint0.Y + aPoint2.Y) * (float)aX +
                (2.0f * aPoint0.Y - 5.0f * aPoint1.Y + 4 * aPoint2.Y - aPoint3.Y) * x2 +
                (-aPoint0.Y + 3.0f * aPoint1.Y - 3.0f * aPoint2.Y + aPoint3.Y) * x3);

            return point;
        }

        private double GetDistance(Vector2 aPosition, Vector2 anOtherPosition)
        {
            return Math.Sqrt(
                Math.Pow(aPosition.X - anOtherPosition.X, 2) +
                Math.Pow(aPosition.Y - anOtherPosition.Y, 2));
        }

        /*
         *  Slider Pathing
         */
        
        private Vector2 GetLinearPathPosition(double aTime)
        {
            double fraction = GetCurveFraction(aTime);
            
            List<double> pathLengths = new List<double>();
            Vector2 previousPosition = Position;
            for(int i = 0; i < nodePositions.Count; ++i)
            {
                // since every node is interpreted as an anchor, we only need to worry about the last node
                // rest will be perfectly followed by just going straight to the node
                double distance = 0;
                if (i < nodePositions.Count - 1)
                {
                    distance          = GetDistance(nodePositions.ElementAt(i), previousPosition);
                    previousPosition  = nodePositions.ElementAt(i);
                }
                else
                    // but if it is the last node, then we need to look at the total length
                    // to see how far it goes in that direction
                    distance = GetCurveLength() - pathLengths.Sum();

                pathLengths.Add(distance);
            }
            
            double fractionDistance = pathLengths.Sum() * fraction;
            int prevNodeIndex = 0;
            foreach(double pathLength in pathLengths)
            {
                if (fractionDistance > pathLength)
                    fractionDistance -= pathLength;
                else
                    break;

                ++prevNodeIndex;
            }
            
            Vector2 startPoint    = nodePositions.ElementAt(prevNodeIndex <= 0 ? 0 : prevNodeIndex - 1);
            Vector2 endPoint      = nodePositions.ElementAt(prevNodeIndex);
            double pointDistance  = GetDistance(startPoint, endPoint);
            double microFraction  = fractionDistance / pointDistance;
            
            return startPoint + new Vector2(
                (endPoint - startPoint).X * (float)microFraction,
                (endPoint - startPoint).Y * (float)microFraction);
        }

        private Vector2 GetPassthroughPathPosition(double aTime)
        {
            // less than 3 interprets as linear
            if (nodePositions.Count < 3)
                return GetLinearPathPosition(aTime);

            // more than 3 interprets as bezier
            if (nodePositions.Count > 3)
                return GetBezierPathPosition(aTime);
            
            Vector2 secondPoint   = nodePositions.ElementAt(1);
            Vector2 thirdPoint    = nodePositions.ElementAt(2);

            // center and radius of the circle
            double divisor = 2 * (UnstackedPosition.X * (secondPoint.Y - thirdPoint.Y) + secondPoint.X *
                (thirdPoint.Y - UnstackedPosition.Y) + thirdPoint.X * (UnstackedPosition.Y - secondPoint.Y));

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

            // which direction to rotate based on which side the center is on
            if (((secondPoint.X - UnstackedPosition.X) * (thirdPoint.Y - UnstackedPosition.Y) - (secondPoint.Y - UnstackedPosition.Y) * (thirdPoint.X - UnstackedPosition.X)) < 0)
                radians *= -1.0f;
            
            // getting the point on the circumference of the circle
            double fraction   = GetCurveFraction(aTime);

            double radianX = Math.Cos(fraction * radians);
            double radianY = Math.Sin(fraction * radians);

            double x = (radianX * (UnstackedPosition.X - centerX)) - (radianY * (UnstackedPosition.Y - centerY)) + centerX;
            double y = (radianY * (UnstackedPosition.X - centerX)) + (radianX * (UnstackedPosition.Y - centerY)) + centerY;

            return new Vector2((float)x, (float)y);
        }

        private List<Vector2> GetBezierPoints()
        {
            // include the first point in the total slider points
            List<Vector2> sliderPoints = nodePositions.ToList();

            Vector2 currentPoint = Position;
            List<Vector2> bezierPoints = new List<Vector2>() { currentPoint };

            // for each anchor, calculate the curve, until we find where we need to be
            int tteration = 0;

            double pixelsPerMs = GetSliderSpeed(time);
            double totalLength = 0;
            double fullLength = GetCurveDuration() * pixelsPerMs;
            
            while (tteration < sliderPoints.Count)
            {
                // get all the nodes from one anchor/start point to the next
                List<Vector2> points = new List<Vector2>();
                int currentIteration = tteration;
                for (int i = currentIteration; i < sliderPoints.Count; ++i)
                {
                    if (i > currentIteration && sliderPoints.ElementAt(i - 1) == sliderPoints.ElementAt(i))
                        break;
                    points.Add(sliderPoints.ElementAt(i));
                    ++tteration;
                }

                // calculate how long this curve (not the whole thing, just from anchor to anchor) will be
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
                            bezierPoints.Add(currentPoint);
                        }
                    }
                }

                // as long as we haven't reached the last path between anchors, keep track of the length of the path
                // ensures that we can switch from one anchor path to another
                if (tteration <= sliderPoints.Count)
                    totalLength += curvePixelLength;
                else
                    bezierPoints.Add(currentPoint);
            }

            return bezierPoints;
        }

        private Vector2 GetBezierPathPosition(double aTime)
        {
            if (bezierPoints == null)
                bezierPoints = GetBezierPoints();

            double fraction = GetCurveFraction(aTime);

            int     integer = (int)Math.Floor(bezierPoints.Count * fraction);
            float   @decimal = (float)(bezierPoints.Count * fraction - integer);
            
            return integer >= bezierPoints.Count - 1
                    ? bezierPoints[bezierPoints.Count - 1]
                    : bezierPoints[integer] + (bezierPoints[integer + 1] - bezierPoints[integer]) * @decimal;
        }

        private Vector2 GetCatmullPathPosition(double aTime)
        {
            // any less than 3 points might as well be linear
            if (nodePositions.Count < 3)
                GetLinearPathPosition(aTime);

            double fraction = GetCurveFraction(aTime);

            double pixelsPerMs = GetSliderSpeed(time);
            double totalLength = 0;
            double desiredLength = GetCurveDuration() * pixelsPerMs * fraction;
            
            List<Vector2> points = new List<Vector2>(nodePositions);
            
            // go through the curve until the fraction is reached
            Vector2 prevPoint = points.First();
            for (int i = 0; i < points.Count - 1; ++i)
            {
                // get the curve length between anchors
                double curvePixelLength = 0;
                Vector2 prevCurvePoint = points[i];
                for (double k = 0.0f; k < 1.0f + stepLength; k += stepLength)
                {
                    Vector2 currentPoint;
                    if (i == 0)
                        // double the start position
                        currentPoint = GetCatmullPoint(points[i], points[i], points[i + 1], points[i + 2], k);
                    else if (i < points.Count - 2)
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 2], k);
                    else
                        // double the end position
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
                    
                    // keeping track of the length of the path ensures that we can switch from one anchor path to another
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
