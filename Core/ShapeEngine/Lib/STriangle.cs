﻿using ShapeEngine.Core;
using System;
using System.Numerics;

namespace ShapeEngine.Lib
{
    public static class STriangle
    {
        public static Triangulation Triangulate(this Triangle t, int pointCount)
        {
            if (pointCount < 0) return new() { t };

            Points points = new() { t.a, t.b, t.c };
            
            for (int i = 0; i < pointCount; i++)
            {
                float f1 = SRNG.randF();
                float f2 = SRNG.randF();
                Vector2 randPoint = GetPoint(t, f1, f2);
                points.Add(randPoint);
            }

            return SPoly.TriangulateDelaunay(points);
        }
        public static Triangulation Triangulate(this Triangle t, float minArea)
        {
            if(minArea <= 0) return new() { t };

            float triArea = t.GetArea();
            float pieceCount = triArea / minArea;
            int points = (int)MathF.Floor((pieceCount - 1f) * 0.5f);
            return t.Triangulate(points);
        }

        public static bool IsNarrow(this Triangle t, float narrowValue = 0.2f)
        {
            Points points = new() { t.a, t.b, t.c };
            for (int i = 0; i < 3; i++)
            {
                Vector2 a = points[i];
                Vector2 b = SUtils.GetItem(points, i + 1);
                Vector2 c = SUtils.GetItem(points, i - 1);

                Vector2 ba = (b - a).Normalize();
                Vector2 ca = (c - a).Normalize();
                float cross = ba.Cross(ca);
                if (MathF.Abs(cross) < narrowValue) return true;
            }
            return false;
        }

        public static Triangulation Triangulate(this Triangle t, Vector2 p)
        {
            return new()
            {
                new(t.a, t.b, p),
                new(t.b, t.c, p),
                new(t.c, t.a, p)
            };
        }
        public static Triangle GetInsideTriangle(this Triangle t, float abF, float bcF, float caF)
        {
            Vector2 a = SVec.Lerp(t.a, t.b, abF);
            Vector2 b = SVec.Lerp(t.b, t.c, bcF);
            Vector2 c = SVec.Lerp(t.c, t.a, caF);
            return new(a, b, c);
        }
        
        /// <summary>
        /// Returns a point inside the triangle.
        /// </summary>
        /// <param name="t">The triangle to find a point in.</param>
        /// <param name="f1">First value in the range 0 - 1.</param>
        /// <param name="f2">Second value in the range 0 - 1.</param>
        /// <returns></returns>
        public static Vector2 GetPoint(this Triangle t, float f1, float f2)
        {
            if((f1 + f2) > 1)
            {
                f1 = 1f - f1;
                f2 = 1f - f2;
            }
            Vector2 ac = (t.c - t.a) * f1;
            Vector2 ab = (t.b - t.a) * f2;
            return t.a + ac + ab;
            //float f1Sq = MathF.Sqrt(f1);
            //float x = (1f - f1Sq) * t.a.X + (f1Sq * (1f - f2)) * t.b.X + (f1Sq * f2) * t.c.X;
            //float y = (1f - f1Sq) * t.a.Y + (f1Sq * (1f - f2)) * t.b.Y + (f1Sq * f2) * t.c.Y;
            //return new(x, y);
        }

        public static Triangle Rotate(this Triangle t, float rad) { return Rotate(t, t.GetCentroid(), rad); }
        public static Triangle Rotate(this Triangle t, Vector2 pivot, float rad)
        {
            Vector2 a = pivot + (t.a - pivot).Rotate(rad);
            Vector2 b = pivot + (t.b - pivot).Rotate(rad);
            Vector2 c = pivot + (t.c - pivot).Rotate(rad);
            return new(a, b, c);
        }
        public static Triangle Scale(this Triangle t, float scale) { return new(t.a * scale, t.b * scale, t.c * scale); }
        public static Triangle Scale(this Triangle t, Vector2 scale) { return new(t.a * scale, t.b * scale, t.c * scale); }
        public static Triangle Scale(this Triangle t, Vector2 pivot, float scale)
        {
            Vector2 a = pivot + (t.a - pivot) * scale;
            Vector2 b = pivot + (t.b - pivot) * scale;
            Vector2 c = pivot + (t.c - pivot) * scale;
            return new(a, b, c);
        }
        public static Triangle Scale(this Triangle t, Vector2 pivot, Vector2 scale)
        {
            Vector2 a = pivot + (t.a - pivot) * scale;
            Vector2 b = pivot + (t.b - pivot) * scale;
            Vector2 c = pivot + (t.c - pivot) * scale;
            return new(a, b, c);
        }
        public static Triangle Scale(this Triangle t, float aF, float bF, float cF) { return new(t.a * aF, t.b * bF, t.c * cF); }
        public static Triangle Scale(this Triangle t, Vector2 aF, Vector2 bF, Vector2 cF) { return new(t.a * aF, t.b * bF, t.c * cF); }
        public static Triangle Move(this Triangle t, Vector2 offset) { return new(t.a + offset, t.b + offset, t.c + offset); }
        public static Triangle Move(this Triangle t, Vector2 aOffset, Vector2 bOffset, Vector2 cOffset) { return new(t.a + aOffset, t.b + bOffset, t.c + cOffset); }
        
        
        
       
        

        //public static List<Segment> GetEdges(this Triangle t) { return new() { new(t.a, t.b), new(t.b, t.c), new(t.c, t.a) }; }
        //public static Polygon GetPointsPolygon(this Triangle t) { return new(t.Centroid, t.a, t.b, t.c); }
        //public static Polygon GetPoints(this Triangle t) { return new() { t.a, t.b, t.c }; }
        //public static SegmentShape GetSegmentsShape(this Triangle t) { return new SegmentShape(t.Centroid, new(t.a, t.b), new(t.b, t.c), new(t.c, t.a)); }
        //public static List<Vector2> GetPoints(this Triangle t) { return new() { t.a, t.b, t.c }; }
        //public static bool IsPointInside(this Triangle t, Vector2 p)
        //{
        //    var triangles = Triangulate(t, p);
        //    float totalArea = triangles.Sum((Triangle t) => { return t.GetArea(); });
        //    return t.GetArea() >= totalArea;
        //
        //}
    }

}
