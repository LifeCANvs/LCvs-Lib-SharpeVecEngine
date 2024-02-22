﻿
using System.Numerics;
using ShapeEngine.Core.Collision;
using ShapeEngine.Core.Structs;
using ShapeEngine.Lib;
using ShapeEngine.Random;

namespace ShapeEngine.Core.Shapes
{
    
    /// <summary>
    /// Points shoud be in CCW order.
    /// </summary>
    public class Polygon : Points, IEquatable<Polygon>
    {
        #region Constructors
        public Polygon() { }
        
        /// <summary>
        /// Points should be in CCW order. Use Reverse if they are in CW order.
        /// </summary>
        /// <param name="points"></param>
        public Polygon(IEnumerable<Vector2> points) { AddRange(points); }
        
        public Polygon(Polygon poly) { AddRange(poly); }
        public Polygon(Polyline polyLine) { AddRange(polyLine); }
        #endregion

        #region Equals & Hashcode
        public bool Equals(Polygon? other)
        {
            if (other == null) return false;
            if (Count != other.Count) return false;
            for (var i = 0; i < Count; i++)
            {
                if (!this[i].IsSimilar(other[i])) return false;
                //if (this[i] != other[i]) return false;
            }
            return true;
        }
        public override int GetHashCode() { return ShapeUtils.GetHashCode(this); }
        #endregion

        #region Public
        public Polygon Project(Vector2 v)
        {
            if (v.LengthSquared() <= 0f) return ToPolygon();
            var translated = Polygon.Move(this, v);
            var points = new Points();
            points.AddRange(this);
            points.AddRange(translated);
            return Polygon.FindConvexHull(points);
        }

        public Polygon ToConvex() => Polygon.FindConvexHull(this);
        public Circle GetBoundingCircle()
        {
            float maxD = 0f;
            int num = this.Count;
            Vector2 origin = new();
            for (int i = 0; i < num; i++) { origin += this[i]; }
            origin = origin / num;
            //origin *= (1f / (float)num);
            for (int i = 0; i < num; i++)
            {
                float d = (origin - this[i]).LengthSquared();
                if (d > maxD) maxD = d;
            }

            return new Circle(origin, MathF.Sqrt(maxD));
        }
        public Rect GetBoundingBox()
        {
            if (Count < 2) return new();
            Vector2 start = this[0];
            Rect r = new(start.X, start.Y, 0, 0);

            foreach (var p in this)
            {
                r = r.Enlarge(p);// ShapeRect.Enlarge(r, p);
            }
            return r;
        }
        
        public Vector2 GetCentroid()
        {
            //return GetCentroidMean();
            Vector2 result = new();
            
            for (int i = 0; i < Count; i++)
            {
                var a = this[i];
                var b = this[(i + 1) % Count];
                //float factor = a.X * b.Y - b.X * a.Y; //clockwise 
                float factor = a.Y * b.X - a.X * b.Y; //counter clockwise
                result.X += (a.X + b.X) * factor;
                result.Y += (a.Y + b.Y) * factor;
            }
            
            return result * (1f / (GetArea() * 6f));
        }
        
        public bool ContainsPoint(Vector2 p) { return IsPointInPoly(this, p); }

        public bool ContainsCollisionObject(CollisionObject collisionObject)
        {
            if (!collisionObject.HasColliders) return false;
            foreach (var collider in collisionObject.Colliders)
            {
                if (!ContainsCollider(collider)) return false;
            }

            return true;
        }
        public bool ContainsCollider(Collider collider)
        {
            switch (collider.GetShapeType())
            {
                case ShapeType.Circle: return ContainsShape(collider.GetCircleShape());
                case ShapeType.Segment: return ContainsShape(collider.GetSegmentShape());
                case ShapeType.Triangle: return ContainsShape(collider.GetTriangleShape());
                case ShapeType.Quad: return ContainsShape(collider.GetQuadShape());
                case ShapeType.Rect: return ContainsShape(collider.GetRectShape());
                case ShapeType.Poly: return ContainsShape(collider.GetPolygonShape());
                case ShapeType.PolyLine: return ContainsShape(collider.GetPolylineShape());
            }

            return false;
        }
        public bool ContainsShape(Segment segment) => ContainsPoints(this, segment.Start, segment.End);
        public bool ContainsShape(Circle circle) => ContainsPoints(this, circle.Top, circle.Left, circle.Bottom, circle.Right);
        public bool ContainsShape(Rect rect) => ContainsPoints(this, rect.TopLeft, rect.BottomLeft, rect.BottomRight, rect.TopRight);
        public bool ContainsShape(Triangle triangle) => ContainsPoints(this, triangle.A, triangle.B, triangle.C);
        public bool ContainsShape(Quad quad) => ContainsPoints(this, quad.A, quad.B, quad.C, quad.D);
        public bool ContainsShape(Points points)
        {
            if (points.Count <= 0) return false;
            foreach (var p in points)
            {
                if (!ContainsPoint(p)) return false;
            }
            return true;
        }

        
        public void FixWindingOrder() { if (this.IsClockwise()) this.Reverse(); }

        public void MakeClockwise()
        {
            if (IsClockwise()) return;
            this.Reverse();
        }

        public void MakeCounterClockwise()
        {
            if (!IsClockwise()) return;
            this.Reverse();
        }
        public void ReduceVertexCount(int newCount)
        {
            if (newCount < 3) Clear();//no points left to form a polygon

            while (Count > newCount)
            {
                float minD = 0f;
                int shortestID = 0;
                for (int i = 0; i < Count; i++)
                {
                    float d = (this[i] - this[(i + 1) % Count]).LengthSquared();
                    if (d > minD)
                    {
                        minD = d;
                        shortestID = i;
                    }
                }
                RemoveAt(shortestID);
            }

        }
        public void ReduceVertexCount(float factor) { ReduceVertexCount(Count - (int)(Count * factor)); }
        public void IncreaseVertexCount(int newCount)
        {
            if (newCount <= Count) return;

            while (Count < newCount)
            {
                float maxD = 0f;
                int longestID = 0;
                for (int i = 0; i < Count; i++)
                {
                    float d = (this[i] - this[(i + 1) % Count]).LengthSquared();
                    if (d > maxD)
                    {
                        maxD = d;
                        longestID = i;
                    }
                }
                Vector2 m = (this[longestID] + this[(longestID + 1) % Count]) * 0.5f;
                this.Insert(longestID + 1, m);
            }
        }
        public Vector2 GetVertex(int index)
        {
            return this[ShapeMath.WrapIndex(Count, index)];
        }
        /// <summary>
        /// Computes the length of this polygon's apothem. This will only be valid if
        /// the polygon is regular. More info: http://en.wikipedia.org/wiki/Apothem
        /// </summary>
        /// <returns>Return the length of the apothem.</returns>
        public float GetApothem()
        {
            return (this.GetCentroid() - (this[0].Lerp(this[1], 0.5f))).Length();
        }
        public Vector2 GetRandomPointConvex()
        {
            var edges = GetEdges();
            var ea = ShapeRandom.RandCollection(edges, true);
            var eb = ShapeRandom.RandCollection(edges);

            var pa = ea.Start.Lerp(ea.End, ShapeRandom.RandF());
            var pb = eb.Start.Lerp(eb.End, ShapeRandom.RandF());
            return pa.Lerp(pb, ShapeRandom.RandF());
        }

        public void Center(Vector2 newCenter)
        {
            var centroid = GetCentroid();
            var delta = newCenter - centroid;
            Move(delta);
        }
        public void Move(Vector2 translation)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i] += translation;
            }
            //return path;
        }
        public void Rotate(Vector2 pivot, float rotRad)
        {
            if (Count < 3) return;
            for (int i = 0; i < Count; i++)
            {
                Vector2 w = this[i] - pivot;
                this[i] = pivot + w.Rotate(rotRad);
            }
            //return path;
        }
        public void Rotate(float rotRad)
        {
            if (Count < 3) return;// new();
            for (int i = 0; i < Count; i++)
            {
                this[i] = this[i].Rotate(rotRad);
            }
            //return path;
        }
        public void Scale(float scale)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i] *= scale;
            }
            //return path;
        }
        public void Scale(Vector2 pivot, float scale)
        {
            if (Count < 3) return;
            for (int i = 0; i < Count; i++)
            {
                Vector2 w = this[i] - pivot;
                this[i] = pivot + w * scale;
            }
        }
        public void Scale(Vector2 pivot, Vector2 scale)
        {
            if (Count < 3) return;// new();
            for (int i = 0; i < Count; i++)
            {
                Vector2 w = this[i] - pivot;
                this[i] = pivot + w * scale;
            }
            //return path;
        }
        public void ScaleUniform(float distance)
        {
            for (int i = 0; i < Count; i++)
            {
                this[i] = ShapeVec.ScaleUniform(this[i], distance);
            }
        }

        public void RemoveColinearVertices()
        {
            if (Count < 3) return;
            Points result = new();
            for (int i = 0; i < Count; i++)
            {
                Vector2 cur = this[i];
                Vector2 prev = ShapeUtils.GetItem(this, i - 1);
                Vector2 next = ShapeUtils.GetItem(this, i + 1);

                Vector2 prevCur = prev - cur;
                Vector2 nextCur = next - cur;
                if (prevCur.Cross(nextCur) != 0f) result.Add(cur);
            }
            Clear();
            AddRange(result);
        }
        public void RemoveDuplicates(float toleranceSquared = 0.001f)
        {
            if (Count < 3) return;
            Points result = new();

            for (int i = 0; i < Count; i++)
            {
                Vector2 cur = this[i];
                Vector2 next = ShapeUtils.GetItem(this, i + 1);
                if ((cur - next).LengthSquared() > toleranceSquared) result.Add(cur);
            }
            Clear();
            AddRange(result);
        }
        public void Smooth(float amount, float baseWeight)
        {
            if (Count < 3) return;
            Points result = new();
            Vector2 centroid = GetCentroid();
            for (int i = 0; i < Count; i++)
            {
                Vector2 cur = this[i];
                Vector2 prev = this[ShapeMath.WrapIndex(Count, i - 1)];
                Vector2 next = this[ShapeMath.WrapIndex(Count, i + 1)];
                Vector2 dir = (prev - cur) + (next - cur) + ((cur - centroid) * baseWeight);
                result.Add(cur + dir * amount);
            }

            Clear();
            AddRange(result);
        }

        public (Polygons newShapes, Polygons cutOuts) Cut(Polygon cutShape)
        {
            var cutOuts = ShapeClipper.Intersect(this, cutShape).ToPolygons(true);
            var newShapes = ShapeClipper.Difference(this, cutShape).ToPolygons(true);

            return (newShapes, cutOuts);
        }
        public (Polygons newShapes, Polygons cutOuts) CutMany(Polygons cutShapes)
        {
            var cutOuts = ShapeClipper.IntersectMany(this, cutShapes).ToPolygons(true);
            var newShapes = ShapeClipper.DifferenceMany(this, cutShapes).ToPolygons(true);
            return (newShapes, cutOuts);
        }
        public (Polygons newShapes, Polygons overlaps) Combine(Polygon other)
        {
            var overlaps = ShapeClipper.Intersect(this, other).ToPolygons(true);
            var newShapes = ShapeClipper.Union(this, other).ToPolygons(true);
            return (newShapes, overlaps);
        }
        public (Polygons newShapes, Polygons overlaps) Combine(Polygons others)
        {
            var overlaps = ShapeClipper.IntersectMany(this, others).ToPolygons(true);
            var newShapes = ShapeClipper.UnionMany(this, others).ToPolygons(true);
            return (newShapes, overlaps);
        }
        public (Polygons newShapes, Polygons cutOuts) CutSimple(Vector2 cutPos, float minCutRadius, float maxCutRadius, int pointCount = 16)
        {
            var cut = Generate(cutPos, pointCount, minCutRadius, maxCutRadius);
            return this.Cut(cut);
        }
        public (Polygons newShapes, Polygons cutOuts) CutSimple(Segment cutLine, float minSectionLength = 0.025f, float maxSectionLength = 0.1f, float minMagnitude = 0.05f, float maxMagnitude = 0.25f)
        {
            var cut = Generate(cutLine, minMagnitude, maxMagnitude, minSectionLength, maxSectionLength);
            return this.Cut(cut);
        }

        public Vector2 GetCentroidMean()
        {
            if (Count <= 0) return new(0f);
            Vector2 total = new(0f);
            foreach (Vector2 p in this) { total += p; }
            return total / Count;
        }
        public Triangulation Triangulate()
        {
            if (Count < 3) return new();
            else if (Count == 3) return new() { new(this[0], this[1], this[2]) };

            Triangulation triangles = new();
            List<Vector2> vertices = new();
            vertices.AddRange(this);
            List<int> validIndices = new();
            for (int i = 0; i < vertices.Count; i++)
            {
                validIndices.Add(i);
            }
            while (vertices.Count > 3)
            {
                if (validIndices.Count <= 0) 
                    break;

                int i = validIndices[ShapeRandom.RandI(0, validIndices.Count)];
                Vector2 a = vertices[i];
                Vector2 b = ShapeUtils.GetItem(vertices, i + 1);
                Vector2 c = ShapeUtils.GetItem(vertices, i - 1);

                Vector2 ba = b - a;
                Vector2 ca = c - a;
                float cross = ba.Cross(ca);
                if (cross >= 0f)//makes sure that ear is not self intersecting
                {
                    validIndices.Remove(i);
                    continue;
                }

                Triangle t = new(a, b, c);

                bool isValid = true;
                foreach (var p in this)
                {
                    if (p == a || p == b || p == c) continue;
                    if (t.ContainsPoint(p))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    triangles.Add(t);
                    vertices.RemoveAt(i);

                    validIndices.Clear();
                    for (int j = 0; j < vertices.Count; j++)
                    {
                        validIndices.Add(j);
                    }
                    //break;
                }
            }


            triangles.Add(new(vertices[0], vertices[1], vertices[2]));


            return triangles;
        }

        /// <summary>
        /// Return the segments of the polygon. If the points are in ccw winding order the normals face outward when InsideNormals = false 
        /// and face inside otherwise.
        /// </summary>
        /// <returns></returns>
        public Segments GetEdges()
        {
            if (Count <= 1) return new();
            if (Count == 2) return new() { new(this[0], this[1]) };
            Segments segments = new();
            for (int i = 0; i < Count; i++)
            {
                segments.Add(new(this[i], this[(i + 1) % Count]));
            }
            return segments;
        }
        
        public Triangle GetBoundingTriangle(float margin = 3f) { return Polygon.GetBoundingTriangle(this, margin); }
        public float GetCircumference() { return MathF.Sqrt(GetCircumferenceSquared()); }
        public float GetCircumferenceSquared()
        {
            if (this.Count < 3) return 0f;
            float lengthSq = 0f;
            for (int i = 0; i < Count; i++)
            {
                Vector2 w = this[(i + 1)%Count] - this[i];
                lengthSq += w.LengthSquared();
            }
            return lengthSq;
        }
        public float GetArea() { return MathF.Abs(GetAreaSigned()); }
        public bool IsClockwise() { return GetAreaSigned() > 0f; }
        public bool IsConvex()
        {
            int num = this.Count;
            bool isPositive = false;

            for (int i = 0; i < num; i++)
            {
                int prevIndex = (i == 0) ? num - 1 : i - 1;
                int nextIndex = (i == num - 1) ? 0 : i + 1;
                var d0 = this[i] - this[prevIndex];
                var d1 = this[nextIndex] - this[i];
                var newIsP = d0.Cross(d1) > 0f;
                if (i == 0) isPositive = true;
                else if (isPositive != newIsP) return false;
            }
            return true;
        }
        public Points ToPoints() { return new(this); }
        
        
        public int GetClosestEdgePointByIndex(Vector2 p)
        {
            if (Count <= 0) return -1;
            if (Count == 1) return 0;

            float minD = float.PositiveInfinity;
            int closestIndex = -1;

            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                var edge = new Segment(start, end);

                Vector2 closest = edge.GetClosestCollisionPoint(p).Point;
                float d = (closest - p).LengthSquared();
                if (d < minD)
                {
                    closestIndex = i;
                    minD = d;
                }
            }
            return closestIndex;
        }
        public ClosestPoint GetClosestPoint(Vector2 p)
        {
            var cp = GetEdges().GetClosestCollisionPoint(p);
            return new(cp, (cp.Point - p).Length());
        }
        public CollisionPoint GetClosestCollisionPoint(Vector2 p) => GetEdges().GetClosestCollisionPoint(p);
        public ClosestSegment GetClosestSegment(Vector2 p) => GetEdges().GetClosest(p);
        
        public Vector2 GetRandomPointInside()
        {
            var triangles = Triangulate();
            List<WeightedItem<Triangle>> items = new();
            foreach (var t in triangles)
            {
                items.Add(new(t, (int)t.GetArea()));
            }
            var item = ShapeRandom.PickRandomItem(items.ToArray());
            return item.GetRandomPointInside();
        }
        public Points GetRandomPointsInside(int amount)
        {
            var triangles = Triangulate();
            WeightedItem<Triangle>[] items = new WeightedItem<Triangle>[triangles.Count];
            for (int i = 0; i < items.Length; i++)
            {
                var t = triangles[i];
                items[i] = new(t, (int)t.GetArea());
            }


            List<Triangle> pickedTriangles = ShapeRandom.PickRandomItems(amount, items);
            Points randomPoints = new();
            foreach (var tri in pickedTriangles) randomPoints.Add(tri.GetRandomPointInside());

            return randomPoints;
        }
        public Vector2 GetRandomVertex() { return ShapeRandom.RandCollection(this); }
        public Segment GetRandomEdge() => GetEdges().GetRandomSegment();
        public Vector2 GetRandomPointOnEdge() => GetRandomEdge().GetRandomPoint();
        public Points GetRandomPointsOnEdge(int amount) => GetEdges().GetRandomPoints(amount);
        
        #endregion

        #region Static
        internal static bool ContainsPointCheck(Vector2 a, Vector2 b, Vector2 pointToCheck)
        {
            if (a.Y < pointToCheck.Y && b.Y >= pointToCheck.Y || b.Y < pointToCheck.Y && a.Y >= pointToCheck.Y)
            {
                if (a.X + (pointToCheck.Y - a.Y) / (b.Y - a.Y) * (b.X - a.X) < pointToCheck.X)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsPointInPoly(Polygon poly, Vector2 p)
        {
            var oddNodes = false;
            int num = poly.Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = poly[i];
                var vj = poly[j];
                if (ContainsPointCheck(vi, vj, p)) oddNodes = !oddNodes;
                j = i;
            }

            return oddNodes;
        }
        public static bool ContainsPoints(Polygon poly, Vector2 a, Vector2 b)
        {
            var oddNodesA = false;
            var oddNodesB = false;
            int num = poly.Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = poly[i];
                var vj = poly[j];
                if(ContainsPointCheck(vi, vj, a)) oddNodesA = !oddNodesA;
                if(ContainsPointCheck(vi, vj, b)) oddNodesB = !oddNodesB;
                
                j = i;
            }

            return oddNodesA || oddNodesB;
        }
        public static bool ContainsPoints(Polygon poly, Vector2 a, Vector2 b, Vector2 c)
        {
            var oddNodesA = false;
            var oddNodesB = false;
            var oddNodesC = false;
            int num = poly.Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = poly[i];
                var vj = poly[j];
                if(ContainsPointCheck(vi, vj, a)) oddNodesA = !oddNodesA;
                if(ContainsPointCheck(vi, vj, b)) oddNodesB = !oddNodesB;
                if(ContainsPointCheck(vi, vj, c)) oddNodesC = !oddNodesC;
                
                j = i;
            }

            return oddNodesA || oddNodesB || oddNodesC;
        }
        public static bool ContainsPoints(Polygon poly, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var oddNodesA = false;
            var oddNodesB = false;
            var oddNodesC = false;
            var oddNodesD = false;
            int num = poly.Count;
            int j = num - 1;
            for (int i = 0; i < num; i++)
            {
                var vi = poly[i];
                var vj = poly[j];
                if(ContainsPointCheck(vi, vj, a)) oddNodesA = !oddNodesA;
                if(ContainsPointCheck(vi, vj, b)) oddNodesB = !oddNodesB;
                if(ContainsPointCheck(vi, vj, c)) oddNodesC = !oddNodesC;
                if(ContainsPointCheck(vi, vj, d)) oddNodesD = !oddNodesD;
                
                j = i;
            }

            return oddNodesA || oddNodesB || oddNodesC || oddNodesD;
        }

        /// <summary>
        /// Triangulates a set of points. Only works with non self intersecting shapes.
        /// </summary>
        /// <param name="points">The points to triangulate. Can be any set of points. (polygons as well) </param>
        /// <returns></returns>
        public static Triangulation TriangulateDelaunay(IEnumerable<Vector2> points)
        {
            var enumerable = points.ToList();
            var supraTriangle = GetBoundingTriangle(enumerable, 2f);
            return TriangulateDelaunay(enumerable, supraTriangle);
        }
        /// <summary>
        /// Triangulates a set of points. Only works with non self intersecting shapes.
        /// </summary>
        /// <param name="points">The points to triangulate. Can be any set of points. (polygons as well) </param>
        /// <param name="supraTriangle">The triangle that encapsulates all the points.</param>
        /// <returns></returns>
        public static Triangulation TriangulateDelaunay(IEnumerable<Vector2> points, Triangle supraTriangle)
        {
            Triangulation triangles = new() { supraTriangle };

            foreach (var p in points)
            {
                Triangulation badTriangles = new();

                //Identify 'bad triangles'
                for (int triIndex = triangles.Count - 1; triIndex >= 0; triIndex--)
                {
                    Triangle triangle = triangles[triIndex];

                    //A 'bad triangle' is defined as a triangle who's CircumCentre contains the current point
                    var circumCircle = triangle.GetCircumCircle();
                    float distSq = Vector2.DistanceSquared(p, circumCircle.Center);
                    if (distSq < circumCircle.Radius * circumCircle.Radius)
                    {
                        badTriangles.Add(triangle);
                        triangles.RemoveAt(triIndex);
                    }
                }

                Segments allEdges = new();
                foreach (var badTriangle in badTriangles) { allEdges.AddRange(badTriangle.GetEdges()); }

                Segments uniqueEdges = GetUniqueSegmentsDelaunay(allEdges);
                //Create new triangles
                for (int i = 0; i < uniqueEdges.Count; i++)
                {
                    var edge = uniqueEdges[i];
                    triangles.Add(new(p, edge));
                }
            }

            //Remove all triangles that share a vertex with the supra triangle to recieve the final triangulation
            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                var t = triangles[i];
                if (t.SharesVertex(supraTriangle)) triangles.RemoveAt(i);
            }


            return triangles;
        }
        private static Segments GetUniqueSegmentsDelaunay(Segments segments)
        {
            Segments uniqueEdges = new();
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var edge = segments[i];
                if (IsSimilar(segments, edge))
                {
                    uniqueEdges.Add(edge);
                }
            }
            return uniqueEdges;
        }
        private static bool IsSimilar(Segments segments, Segment seg)
        {
            var counter = 0;
            foreach (var segment in segments)
            {
                if (segment.IsSimilar(seg)) counter++;
                if (counter > 1) return false;
            }
            return true;
        }
        

        /// <summary>
        /// Get a rect that encapsulates all points.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Rect GetBoundingBox(IEnumerable<Vector2> points)
        {
            var enumerable = points as Vector2[] ?? points.ToArray();
            if (enumerable.Length < 2) return new();
            var start = enumerable.First();
            Rect r = new(start.X, start.Y, 0, 0);

            foreach (var p in enumerable)
            {
                r = r.Enlarge(p);
            }
            return r;
        }
        /// <summary>
        /// Get a triangle the encapsulates all points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="marginFactor"> A factor for scaling the final triangle.</param>
        /// <returns></returns>
        public static Triangle GetBoundingTriangle(IEnumerable<Vector2> points, float marginFactor = 1f)
        {
            var bounds = GetBoundingBox(points);
            float dMax = ShapeVec.Max(bounds.Size) * marginFactor; // SVec.Max(bounds.BottomRight - bounds.BottomLeft) + margin; //  Mathf.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY) * Margin;
            Vector2 center = bounds.Center;

            ////The float 0.866 is an arbitrary value determined for optimum supra triangle conditions.
            //float x1 = center.X - 0.866f * dMax;
            //float x2 = center.X + 0.866f * dMax;
            //float x3 = center.X;
            //
            //float y1 = center.Y - 0.5f * dMax;
            //float y2 = center.Y - 0.5f * dMax;
            //float y3 = center.Y + dMax;
            //
            //Vector2 a = new(x1, y1);
            //Vector2 b = new(x2, y2);
            //Vector2 c = new(x3, y3);

            Vector2 a = new Vector2(center.X, bounds.BottomLeft.Y + dMax);
            Vector2 b = new Vector2(center.X - dMax * 1.25f, bounds.TopLeft.Y - dMax / 4);
            Vector2 c = new Vector2(center.X + dMax * 1.25f, bounds.TopLeft.Y - dMax / 4);


            return new Triangle(a, b, c);
        }
        
        
        public static List<Vector2> GetSegmentAxis(Polygon p, bool normalized = false)
        {
            if (p.Count <= 1) return new();
            else if (p.Count == 2)
            {
                return new() { p[1] - p[0] };
            }
            List<Vector2> axis = new();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 start = p[i];
                Vector2 end = p[(i + 1) % p.Count];
                Vector2 a = end - start;
                axis.Add(normalized ? ShapeVec.Normalize(a) : a);
            }
            return axis;
        }
        public static List<Vector2> GetSegmentAxis(Segments edges, bool normalized = false)
        {
            List<Vector2> axis = new();
            foreach (var seg in edges)
            {
                axis.Add(normalized ? seg.Dir : seg.Displacement);
            }
            return axis;
        }

        public static Polygon GetShape(Points relative, Transform2D transform)
        {
            if (relative.Count < 3) return new();
            Polygon shape = new();
            for (int i = 0; i < relative.Count; i++)
            {
                shape.Add(transform.Apply(relative[i]));
                // shape.Add(pos + ShapeVec.Rotate(relative[i], rotRad) * scale);
            }
            return shape;
        }
        public static Points GenerateRelative(int pointCount, float minLength, float maxLength)
        {
            Points points = new();
            float angleStep = ShapeMath.PI * 2.0f / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float randLength = ShapeRandom.RandF(minLength, maxLength);
                Vector2 p = ShapeVec.Rotate(ShapeVec.Right(), -angleStep * i) * randLength;
                points.Add(p);
            }
            return points;
        }
        
        public static Polygon Generate(Vector2 center, int pointCount, float minLength, float maxLength)
        {
            Polygon points = new();
            float angleStep = ShapeMath.PI * 2.0f / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float randLength = ShapeRandom.RandF(minLength, maxLength);
                Vector2 p = ShapeVec.Rotate(ShapeVec.Right(), -angleStep * i) * randLength;
                p += center;
                points.Add(p);
            }
            return points;
        }
        /// <summary>
        /// Generates a polygon around the given segment. Points are generated ccw around the segment beginning with the segment start.
        /// </summary>
        /// <param name="segment">The segment to build a polygon around.</param>
        /// <param name="magMin">The minimum perpendicular magnitude factor for generating a point. (0-1)</param>
        /// <param name="magMax">The maximum perpendicular magnitude factor for generating a point. (0-1)</param>
        /// <param name="minSectionLength">The minimum factor of the length between points along the line.(0-1)</param>
        /// <param name="maxSectionLength">The maximum factor of the length between points along the line.(0-1)</param>
        /// <returns>Returns the a generated polygon.</returns>
        public static Polygon Generate(Segment segment, float magMin = 0.1f, float magMax = 0.25f, float minSectionLength = 0.025f, float maxSectionLength = 0.1f)
        {
            Polygon poly = new() { segment.Start };
            var dir = segment.Dir;
            var dirRight = dir.GetPerpendicularRight();
            var dirLeft = dir.GetPerpendicularLeft();
            float len = segment.Length;
            float minSectionLengthSq = (minSectionLength * len) * (minSectionLength * len);
            Vector2 cur = segment.Start;
            while (true)
            {
                cur += dir * ShapeRandom.RandF(minSectionLength, maxSectionLength) * len;
                if ((cur - segment.End).LengthSquared() < minSectionLengthSq) break;
                poly.Add(cur + dirRight * ShapeRandom.RandF(magMin, magMax));
            }
            cur = segment.End;
            poly.Add(cur);
            while (true)
            {
                cur -= dir * ShapeRandom.RandF(minSectionLength, maxSectionLength) * len;
                if ((cur - segment.Start).LengthSquared() < minSectionLengthSq) break;
                poly.Add(cur + dirLeft * ShapeRandom.RandF(magMin, magMax));
            }
            return poly;
        }

        public static Polygon Center(Polygon p, Vector2 newCenter)
        {
            var centroid = p.GetCentroid();
            var delta = newCenter - centroid;
            return Move(p, delta);
        }
        public static Polygon Move(Polygon p, Vector2 translation)
        {
            Polygon result = new();
            for (int i = 0; i < p.Count; i++)
            {
                result.Add(p[i] + translation);
            }
            return result;
        }
        
        public static Polygon Rotate(Polygon p, Vector2 pivot, float rotRad)
        {
            if (p.Count < 3) return new();
            Polygon rotated = new();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 w = p[i] - pivot;
                rotated.Add(pivot + w.Rotate(rotRad));
            }
            return rotated;
        }
        public static Polygon Scale(Polygon p, float scale)
        {
            Polygon shape = new();
            for (int i = 0; i < p.Count; i++)
            {
                shape.Add(p[i] * scale);
            }
            return shape;
        }
        public static Polygon Scale(Polygon p, Vector2 pivot, float scale)
        {
            if (p.Count < 3) return new();
            Polygon scaled = new();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 w = p[i] - pivot;
                scaled.Add(pivot + w * scale);
            }
            return scaled;
        }
        public static Polygon Scale(Polygon p, Vector2 pivot, Vector2 scale)
        {
            if (p.Count < 3) return new();
            Polygon scaled = new();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 w = p[i] - pivot;
                scaled.Add(pivot + w * scale);
            }
            return scaled;
        }
        public static Polygon ScaleUniform(Polygon p, float distance)
        {
            Polygon shape = new();
            for (int i = 0; i < p.Count; i++)
            {
                shape.Add(ShapeVec.ScaleUniform(p[i], distance));
            }
            return shape;
        }
        #endregion
        
        #region Private
        private float GetAreaSigned()
        {
            float totalArea = 0f;

            for (int i = 0; i < this.Count; i++)
            {
                Vector2 a = this[i];
                Vector2 b = this[(i + 1) % this.Count];

                float dy = (a.Y + b.Y) / 2f;
                float dx = b.X - a.X;

                float area = dy * dx;
                totalArea += area;
            }

            return totalArea;
        }
        #endregion

        #region Overlap
        
        public bool Overlap(Collider collider)
        {
            if (!collider.Enabled) return false;

            switch (collider.GetShapeType())
            {
                case ShapeType.Circle:
                    var c = collider.GetCircleShape();
                    return OverlapShape(c);
                case ShapeType.Segment:
                    var s = collider.GetSegmentShape();
                    return OverlapShape(s);
                case ShapeType.Triangle:
                    var t = collider.GetTriangleShape();
                    return OverlapShape(t);
                case ShapeType.Rect:
                    var r = collider.GetRectShape();
                    return OverlapShape(r);
                case ShapeType.Quad:
                    var q = collider.GetQuadShape();
                    return OverlapShape(q);
                case ShapeType.Poly:
                    var p = collider.GetPolygonShape();
                    return OverlapShape(p);
                case ShapeType.PolyLine:
                    var pl = collider.GetPolylineShape();
                    return OverlapShape(pl);
            }

            return false;
        }
        public bool OverlapShape(Segment s) => s.OverlapShape(this);
        public bool OverlapShape(Circle c) => c.OverlapShape(this);
        public bool OverlapShape(Triangle t) => t.OverlapShape(this);
        public bool OverlapShape(Rect r) => r.OverlapShape(this);
        public bool OverlapShape(Quad q) => q.OverlapShape(this);
        public bool OverlapShape(Polygon b)
        {
            if (Count < 3 || b.Count < 3) return false;
            
            var oddNodesThis = false;
            var oddNodesB = false;
            var containsPointBCheckFinished = false;

            var pointToCeckThis = this[0];
            var pointToCeckB = b[0];
            
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                
                for (int j = 0; j < b.Count; j++)
                {
                    var bStart = b[j];
                    var bEnd = b[(j + 1) % b.Count];
                    if (Segment.OverlapSegmentSegment(start, end, bStart, bEnd)) return true;
                    
                    if (containsPointBCheckFinished) continue;
                    if(Polygon.ContainsPointCheck(bStart, bEnd, pointToCeckThis)) oddNodesB = !oddNodesB;
                }

                if (!containsPointBCheckFinished)
                {
                    if (oddNodesB) return true;
                    containsPointBCheckFinished = true;
                }
               
                if(Polygon.ContainsPointCheck(start, end, pointToCeckB)) oddNodesThis = !oddNodesThis;
            }

            return oddNodesThis || oddNodesB;
        }
        public bool OverlapShape(Polyline pl)
        {
            if (Count < 3 || pl.Count < 2) return false;
            
            var oddNodes = false;
            var pointToCeck = pl[0];

            
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];
                
                for (int j = 0; j < pl.Count - 1; j++)
                {
                    var bStart = pl[j];
                    var bEnd = pl[(j + 1) % pl.Count];
                    if (Segment.OverlapSegmentSegment(start, end, bStart, bEnd)) return true;
                }

                if(Polygon.ContainsPointCheck(start, end, pointToCeck)) oddNodes = !oddNodes;
            }

            return oddNodes;
        }
        public bool OverlapShape(Segments segments)
        {
            if (Count < 3 || segments.Count <= 0) return false;
            
            var oddNodes = false;
            var pointToCeck = segments[0].Start;

            
            for (var i = 0; i < Count; i++)
            {
                var start = this[i];
                var end = this[(i + 1) % Count];

                foreach (var seg in segments)
                {
                    if (Segment.OverlapSegmentSegment(start, end, seg.Start, seg.End)) return true;
                }

                if(Polygon.ContainsPointCheck(start, end, pointToCeck)) oddNodes = !oddNodes;
            }

            return oddNodes;
        }


        #endregion

        #region Intersect
        public CollisionPoints? Intersect(Collider collider)
        {
            if (!collider.Enabled) return null;

            switch (collider.GetShapeType())
            {
                case ShapeType.Circle:
                    var c = collider.GetCircleShape();
                    return IntersectShape(c);
                case ShapeType.Segment:
                    var s = collider.GetSegmentShape();
                    return IntersectShape(s);
                case ShapeType.Triangle:
                    var t = collider.GetTriangleShape();
                    return IntersectShape(t);
                case ShapeType.Rect:
                    var r = collider.GetRectShape();
                    return IntersectShape(r);
                case ShapeType.Quad:
                    var q = collider.GetQuadShape();
                    return IntersectShape(q);
                case ShapeType.Poly:
                    var p = collider.GetPolygonShape();
                    return IntersectShape(p);
                case ShapeType.PolyLine:
                    var pl = collider.GetPolylineShape();
                    return IntersectShape(pl);
            }

            return null;
        }
        public CollisionPoints? IntersectShape(Segment s)
        {
            if (Count < 3) return null;
            CollisionPoints? points = null;
            CollisionPoint? colPoint = null;
            for (var i = 0; i < Count; i++)
            {
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],s.Start, s.End);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Circle c)
        {
            if (Count < 3) return null;
            
            CollisionPoints? points = null;
            (CollisionPoint? a, CollisionPoint? b) result;
            
            for (var i = 0; i < Count; i++)
            {
                result = Segment.IntersectSegmentCircle(this[i], this[(i + 1) % Count], c.Center, c.Radius);
                if (result.a != null || result.b != null)
                {
                    points ??= new();
                    if(result.a != null) points.Add((CollisionPoint)result.a);
                    if(result.b != null) points.Add((CollisionPoint)result.b);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Triangle t)
        {
            if (Count < 3) return null;

            CollisionPoints? points = null;
            CollisionPoint? colPoint = null;
            for (var i = 0; i < Count; i++)
            {
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], t.A, t.B);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], t.B, t.C);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], t.C, t.A);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Rect r)
        {
            if (Count < 3) return null;

            CollisionPoints? points = null;

            var a = r.TopLeft;
            var b = r.BottomLeft;
            var c = r.BottomRight;
            var d = r.TopRight;
            
            for (var i = 0; i < Count; i++)
            {
                var colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], a, b);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], b, c);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], c, d);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                colPoint = Segment.IntersectSegmentSegment( this[i], this[(i + 1) % Count], d, a);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Quad q)
        {
            if (Count < 3) return null;

            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],q.A, q.B);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count], q.B, q.C);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],q.C, q.D);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
                colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],q.D, q.A);
                if (colPoint != null)
                {
                    points ??= new();
                    points.Add((CollisionPoint)colPoint);
                }
                
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Polygon b)
        {
            if (Count < 3 || b.Count < 3) return null;
            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                for (var j = 0; j < b.Count; j++)
                {
                    var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],b[j], b[(j + 1) % b.Count]);
                    if (colPoint != null)
                    {
                        points ??= new();
                        points.Add((CollisionPoint)colPoint);
                    }
                }
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Polyline pl)
        {
            if (Count < 3 || pl.Count < 2) return null;
            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                for (var j = 0; j < pl.Count - 1; j++)
                {
                    var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],pl[j], pl[(j + 1) % pl.Count]);
                    if (colPoint != null)
                    {
                        points ??= new();
                        points.Add((CollisionPoint)colPoint);
                    }
                }
            }
            return points;
        }
        public CollisionPoints? IntersectShape(Segments segments)
        {
            if (Count < 3 || segments.Count <= 0) return null;
            CollisionPoints? points = null;
            for (var i = 0; i < Count; i++)
            {
                foreach (var seg in segments)
                {
                    var colPoint = Segment.IntersectSegmentSegment(this[i], this[(i + 1) % Count],seg.Start, seg.End);
                    if (colPoint != null)
                    {
                        points ??= new();
                        points.Add((CollisionPoint)colPoint);
                    }
                }
            }
            return points;
        }

        #endregion

        #region Convex Hull
        //ALternative algorithms
            //https://en.wikipedia.org/wiki/Graham_scan
            //https://en.wikipedia.org/wiki/Chan%27s_algorithm
            
        //GiftWrapping
        //https://www.youtube.com/watch?v=YNyULRrydVI -> coding train
        //https://en.wikipedia.org/wiki/Gift_wrapping_algorithm -> wiki
        public static Polygon FindConvexHull(List<Vector2> points) => ConvexHull_JarvisMarch(points);
        public static Polygon FindConvexHull(Points points) => ConvexHull_JarvisMarch(points);
        public static Polygon FindConvexHull(params Vector2[] points) => ConvexHull_JarvisMarch(points.ToList());
        public static Polygon FindConvexHull(Polygon points) => ConvexHull_JarvisMarch(points);
        public static Polygon FindConvexHull(params Polygon[] shapes)
        {
            var allPoints = new List<Vector2>();
            foreach (var shape in shapes)
            {
                allPoints.AddRange(shape);
            }
            return ConvexHull_JarvisMarch(allPoints);
        }
        
        #endregion
        
        #region Jarvis March Algorithm (Find Convex Hull)

        //SOURCE https://github.com/allfii/ConvexHull/tree/master
        
        private static int Turn_JarvisMarch(Vector2 p, Vector2 q, Vector2 r)
        {
            return ((q.X - p.X) * (r.Y - p.Y) - (r.X - p.X) * (q.Y - p.Y)).CompareTo(0);
            // return ((q.getX() - p.getX()) * (r.getY() - p.getY()) - (r.getX() - p.getX()) * (q.getY() - p.getY())).CompareTo(0);
        }
        private static Vector2 NextHullPoint_JarvisMarch(List<Vector2> points, Vector2 p)
        {
            // const int TurnLeft = 1;
            const int turnRight = -1;
            const int turnNone = 0;
            var q = p;
            int t;
            foreach (var r in points)
            {
                t = Turn_JarvisMarch(p, q, r);
                if (t == turnRight || t == turnNone && p.DistanceSquared(r) > p.DistanceSquared(q)) // dist(p, r) > dist(p, q))
                    q = r;
            }
            return q;
        }
        private static Polygon ConvexHull_JarvisMarch(List<Vector2> points)
        {
            var hull = new List<Vector2>();
            foreach (var p in points)
            {
                if (hull.Count == 0)
                    hull.Add(p);
                else
                {
                    if (hull[0].X > p.X)
                        hull[0] = p;
                    else if (ShapeMath.EqualsF(hull[0].X, p.X))
                        if (hull[0].Y > p.Y)
                            hull[0] = p;
                }
            }
            var counter = 0;
            while (counter < hull.Count)
            {
                var q = NextHullPoint_JarvisMarch(points, hull[counter]);
                if (q != hull[0])
                {
                    hull.Add(q);
                }
                counter++;
            }
            return new Polygon(hull);
        }
        #endregion
    }
}

