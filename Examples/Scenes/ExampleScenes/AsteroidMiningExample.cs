﻿using Clipper2Lib;
using Raylib_CsLo;
using ShapeEngine.Core;
using ShapeEngine.Lib;
using ShapeEngine.Screen;
using System.Numerics;
using ShapeEngine.Core.Collision;
using ShapeEngine.Core.Interfaces;
using ShapeEngine.Core.Shapes;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes
{
    public abstract class SpaceObject : IGameObject
    {
        protected bool dead = false;
        public int Layer { get; set; } = 0;
        public bool Kill()
        {
            if (dead) return false;
            dead = true;
            return true;
        }

        public abstract Vector2 GetPosition();
        public abstract Rect GetBoundingBox();

        public virtual void Update(float dt, ScreenInfo game, ScreenInfo ui) { }
        public virtual void DrawGame(ScreenInfo game) { }
        public virtual void DrawUI(ScreenInfo ui) { }
        public virtual void Overlap(CollisionInformation info) { }
        public virtual void OverlapEnded(ICollidable other) { }
        public virtual void AddedToHandler(GameObjectHandler gameObjectHandler) { }
        public virtual void RemovedFromHandler(GameObjectHandler gameObjectHandler) { }
        
        public void DeltaFactorApplied(float f) { }
        
        public bool IsDead() { return dead; }
        public bool DrawToGame(Rect gameArea) { return true; }
        public bool DrawToUI(Rect uiArea) { return false; }
        public bool CheckHandlerBounds() { return false; }
        public void LeftHandlerBounds(Vector2 safePosition, CollisionPoints collisionPoints) { }

        
    }
    public class AsteroidShard : SpaceObject
    {
        private Polygon shape;
        private Vector2 pos;
        private Vector2 vel;
        private float rotDeg = 0f;
        private float angularVelDeg = 0f;
        private float lifetimeTimer = 0f;
        private float lifetime = 0f;
        private float lifetimeF = 1f;

        private float delay = 1f;

        private Color color;
        public AsteroidShard(Polygon shape, Vector2 fractureCenter, Color color)
        {
            this.shape = shape;
            this.rotDeg = 0f;
            this.pos = shape.GetCentroid();
            Vector2 dir = (pos - fractureCenter).Normalize();
            this.vel = dir * SRNG.randF(100, 300);
            this.angularVelDeg = SRNG.randF(-90, 90);
            this.lifetime = SRNG.randF(1.5f, 3f);
            this.lifetimeTimer = this.lifetime;
            this.delay = 0.5f;
            this.color = color;
            this.color = YELLOW;
            //this.delay = SRNG.randF(0.25f, 1f);
            //this.lifetime = delay * 3f;
        }
        public override void Update(float dt, ScreenInfo game, ScreenInfo ui)
        {
            if(lifetimeTimer > 0f)
            {
                lifetimeTimer -= dt;
                if(lifetimeTimer <= 0f)
                {
                    lifetimeTimer = 0f;
                    dead = true;
                }
                else
                {
                    lifetimeF = lifetimeTimer / lifetime;

                    if (lifetime - lifetimeTimer > delay)
                    {
                        
                        float prevRotDeg = rotDeg;
                        pos += vel * dt;
                        rotDeg += angularVelDeg * dt;

                        float rotDifDeg = rotDeg - prevRotDeg;
                        shape.Center(pos);
                        shape.Rotate(new Vector2(0.5f), rotDifDeg * DEG2RAD);
                    }
                    
                }
            }
        }
        public override void DrawGame(ScreenInfo game)
        {
            //SDrawing.DrawCircleFast(pos, 4f, RED);
            Color color = this.color.ChangeAlpha((byte)(255 * lifetimeF));
            //color = this.color;
            shape.DrawLines(2f * lifetimeF, color);
        }
        public override Rect GetBoundingBox() { return shape.GetBoundingBox(); }
        public override Vector2 GetPosition() { return pos; }
    }
    public class Asteroid : SpaceObject, ICollidable
    {
        internal class DamagedSegment
        {

            public Segment Segment;
            private float timer;
            private const float Lifetime = 1f;
            public DamagedSegment(Segment segment)
            {
                this.Segment = segment;
                this.timer = Lifetime;
            }
            public bool IsFinished() { return timer <= 0f; }
            public void Update(float dt)
            {
                if (timer > 0f)
                {
                    timer -= dt;
                    if (timer <= 0f) timer = 0f;
                }
            }
            public void Draw()
            {
                float f = timer / Lifetime;
                //Color color = YELLOW.ChangeAlpha((byte)(255 * f));
                Segment.Draw(SRNG.randF(4, 8) * f, YELLOW, 12);
            }
            public void Renew() { timer = Lifetime; }
        }
        internal class DamagedSegments : List<DamagedSegment>
        {
            public void AddSegment(Segment segment)
            {
                foreach (var seg in this)
                {
                    if (seg.Segment.Equals(segment))
                    {
                        seg.Renew();
                        return;
                    }
                }

                Add(new(segment));
            }
            public bool ContainsSegment(Segment segment)
            {
                foreach (var seg in this)
                {
                    if (seg.Segment.Equals(segment)) return true;
                }
                return false;
            }

            public void Update(float dt)
            {
                if (Count > 0)
                {
                    for (int i = Count - 1; i >= 0; i--)
                    {
                        var seg = this[i];
                        seg.Update(dt);
                        if (seg.IsFinished()) this.RemoveAt(i);
                    }
                }
            }
            public void Draw()
            {
                foreach (var seg in this)
                {
                    seg.Draw();
                }
            }
        }
        
        private const float DamageThreshold = 50f;

        private PolyCollider collider;
        private List<ICollidable> collidables = new();
        private uint[] colMask = new uint[] { };
        private bool overlapped = false;
        private float curThreshold = DamageThreshold;

        public event Action<Asteroid, Vector2>? Fractured;
        private Color curColor = RED;

        private DamagedSegments damagedSegments = new();

        public Asteroid(Vector2 pos, params Vector2[] shape)
        {
            collider = new PolyCollider(pos, new(), shape);
            collider.ComputeCollision = false;
            collider.ComputeIntersections = false;
            collidables.Add(this);
            SetDamageTreshold(0f);
        }
        public Asteroid(Polygon shape)
        {
            collider = new PolyCollider(shape);
            collider.ComputeCollision = false;
            collider.ComputeIntersections = false;
            collidables.Add(this);
            SetDamageTreshold(0f);
        }
        public Polygon GetPolygon() { return collider.GetPolygonShape(); }

        private void SetDamageTreshold(float overshoot = 0f)
        {
            curThreshold = DamageThreshold * SRNG.randF(0.5f, 2f) + overshoot;
        }
        public void Overlapped()
        {
            overlapped = true;
        }
        public Color GetColor()
        {
            return curColor;
        }
        public void Damage(float amount, Vector2 point)
        {
            //find segments close to point
            //fade the color from impact color to cur color over several segments
            if (amount <= 0) return;


            var shape = collider.GetPolygonShape();
            var seg = shape.GetClosestSegment(point);
            damagedSegments.AddSegment(seg.Segment);

            curThreshold -= amount;
            if(curThreshold <= 0f)
            {
                SetDamageTreshold(MathF.Abs(curThreshold));
                Fractured?.Invoke(this, point);
                
                //cut piece
                //var cutShape = SPoly.Generate(point, SRNG.randI(6, 12), 50, 250);
                
            }
        }

        public override void Update(float dt, ScreenInfo game, ScreenInfo ui) 
        {
            damagedSegments.Update(dt);
        }
        public override void DrawGame(ScreenInfo game)
        {
            //Color color = overlapped ? GREEN : WHITE;
            //collider.GetShape().DrawShape(4f, color);
            
            if(collider.GetShape() is Polygon p)
            {
                if (overlapped)
                {
                    curColor = GREEN;
                    p.DrawLines(6f, GREEN, 12);
                }
                else
                {
                    curColor = RED;
                    p.DrawLines(3f, RED, 12);
                }
                //p.DrawVertices(4f, RED);
            }

            damagedSegments.Draw();

            overlapped = false;
        }
        
        public virtual bool HasCollidables() { return true; }
        public virtual List<ICollidable> GetCollidables() { return collidables; }

        public override Rect GetBoundingBox() { return collider.GetShape().GetBoundingBox(); }
        public override Vector2 GetPosition() { return collider.Pos; }
        public ICollider GetCollider() { return collider; }
        public uint GetCollisionLayer() { return AsteroidMiningExample.AsteriodLayer; }
        public uint[] GetCollisionMask() { return colMask; }
    }

    public class LaserDevice : SpaceObject
    {
        private const float LaserRange = 1200;
        private const float DamagePerSecond = 50;
        private bool aimingMode = true;
        private bool hybernate = false;
        private bool laserEnabled = false;

        private Vector2 pos;
        private float rotRad;
        private float size;
        private Triangle shape;

        private Vector2 tip;
        private Points laserPoints = new();
        //private Vector2 laserEndPoint;
        private Vector2 aimDir = new();
        private GameObjectHandlerCollision gameObjectHandler;
        public LaserDevice(Vector2 pos, float size, GameObjectHandlerCollision gameObjectHandler) 
        {
            this.gameObjectHandler = gameObjectHandler;
            this.pos = pos;
            this.size = size;
            this.rotRad = 0f;
            UpdateTriangle();
            //this.laserEndPoint = tip;
        }
        public void SetHybernate(bool enabled)
        {
            if (enabled)
            {
                aimingMode = true;
                hybernate = true;
            }
            else
            {
                aimingMode = true;
                hybernate = false;
            }
            
        }
        public void SetAimingMode(bool enabled)
        {
            aimingMode = enabled;

        }
        
        private void UpdateTriangle()
        {
            Vector2 a = pos + new Vector2(size / 2, 0f).Rotate(rotRad);
            Vector2 b = pos + new Vector2(-size / 2, -size / 4).Rotate(rotRad);
            Vector2 c = pos + new Vector2(-size / 2, size / 4).Rotate(rotRad);

            this.shape = new Triangle(a, b, c);
            this.tip = a;
        }

        public override void Update(float dt, ScreenInfo game, ScreenInfo ui)
        {
            laserPoints.Clear();
            laserEnabled = false;
            if (hybernate) return;
            
            if (aimingMode)
            {
                Vector2 dir = game.MousePos - pos;
                aimDir = dir.Normalize();
                rotRad = dir.AngleRad();

                laserEnabled = IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT);
            }
            else
            {
                pos = game.MousePos;
            }
            
            UpdateTriangle();

            if (laserEnabled)
            {
                //laserEndPoint = tip + aimDir * LaserRange;
                var col = gameObjectHandler.GetCollisionHandler();
                if(col != null)
                {
                    laserPoints.Add(tip);
                    
                    float remainingLength = LaserRange;
                    Vector2 curLaserPos = tip;
                    Vector2 curLaserDir = aimDir;
                    float curDamagePerSecond = DamagePerSecond;
                    while(remainingLength > 0) //reflecting laser
                    {
                        var result = CastLaser(dt, curLaserPos, curLaserDir, remainingLength, curDamagePerSecond, col);
                        laserPoints.Add(result.endPoint);
                        curLaserPos = result.endPoint;
                        remainingLength = result.remainingLength;
                        curLaserDir = result.newDir;
                        curDamagePerSecond *= 0.5f;
                        remainingLength *= 0.5f;
                        remainingLength = 0f; //reflecting turned off
                    }
                    
                }

            }
        }
        private (Vector2 endPoint, float remainingLength, Vector2 newDir) CastLaser(float dt, Vector2 start, Vector2 dir, float length, float damagePerSecond, CollisionHandler col)
        {
            Vector2 endPoint = start + dir * length;
            Vector2 newEndPoint = endPoint;
            Vector2 newDir = dir;

            var queryInfos = col.QuerySpace(new Segment(start, endPoint), start, true, AsteroidMiningExample.AsteriodLayer);
            if (queryInfos.Count > 0)
            {
                var closest = queryInfos[0];
                if (closest.Points.Valid)
                {
                    var other = closest.Collidable;
                    if (other != null && other is Asteroid a)
                    {
                        //perfect naming:)
                        newDir = dir.Reflect(closest.Points.Closest.Normal);
                        newEndPoint = closest.Points.Closest.Point;  //closest.intersection.ColPoints[0].Point;
                        a.Damage(damagePerSecond * dt, newEndPoint);
                    }
                }
            }

            float usedLength = (newEndPoint - start).Length();
            //if (usedLength < 10) return (newEndPoint, 0, dir);

            float remainingLength = length - usedLength;
            if (remainingLength <= 1) return new(newEndPoint, 0f, dir);
            else return (newEndPoint - dir * 10f, remainingLength, newDir);
            //return (newEndPoint, remainingLength, newDir);
        }

        public override void DrawGame(ScreenInfo game)
        {
            if (hybernate) return;
            shape.DrawLines(4f, RED);
            SDrawing.DrawCircle(tip, 8f, RED);

            if (laserEnabled && laserPoints.Count > 1)
            {
                for (int i = 0; i < laserPoints.Count - 1; i++)
                {
                    Segment laserSegment = new(laserPoints[i], laserPoints[i + 1]);
                    laserSegment.Draw(4f, RED);
                    SDrawing.DrawCircle(laserPoints[i + 1], SRNG.randF(6f, 12f), RED, 12);
                }
                
            }


        }


        public override Rect GetBoundingBox() { return shape.GetBoundingBox(); }
        public override Vector2 GetPosition() { return pos; }
    }
    
    public class AsteroidMiningExample : ExampleScene
    {
        internal class Cutout
        {

            private Polygon shape;
            private float timer;
            private const float Lifetime = 0.5f;
            public Cutout(Polygon shape)
            {
                this.shape = shape;
                this.timer = Lifetime;
            }
            public bool IsFinished() { return timer <= 0f; }
            public void Update(float dt)
            {
                if (timer > 0f)
                {
                    timer -= dt;
                    if (timer <= 0f) timer = 0f;
                }
            }
            public void Draw()
            {
                float f = timer / Lifetime;
                //Color color = YELLOW.ChangeAlpha((byte)(255 * f));
                shape.DrawLines(6f * f, YELLOW);
            }
        }

        public static uint AsteriodLayer = 1;
        private const float MinPieceArea = 3000f;

        internal enum ShapeType { None = 0, Triangle = 1, Rect = 2, Poly = 3};

        private Font font;
        private GameObjectHandlerCollision gameObjectHandler;
        private Rect boundaryRect = new();

        private bool polyModeActive = false;
        private ShapeType curShapeType = ShapeType.None;

        private Polygon curShape = new();
        private List<Cutout> lastCutOuts = new();
        private Vector2 curPos = new();
        private float curRot = 0f;
        private float curSize = 50;

        private LaserDevice laserDevice;

        private FractureHelper fractureHelper = new(250, 1500, 0.75f, 0.1f);

        //private float crossResult = 0f;

        //Polygons testShapes = new();
        //Rect clipRect = new();
        //RectD clipperRect = new();
        public AsteroidMiningExample()
        {
            Title = "Asteroid Mining Example";
            font = GAMELOOP.GetFont(FontIDs.JetBrains);
            UpdateBoundaryRect(GAMELOOP.Game.Area);
            gameObjectHandler = new GameObjectHandlerCollision(boundaryRect, 4, 4);

            laserDevice = new(new Vector2(0f), 100, gameObjectHandler);
            gameObjectHandler.AddAreaObject(laserDevice);

            //testShapes.Add(SPoly.Generate(new Vector2(0f), 24, 50, 300));
        }
        public override void Reset()
        {
            gameObjectHandler.Clear();
            polyModeActive = false;
            curRot = 0f;
            curSize = 50f;
            curShapeType = ShapeType.Triangle;
            RegenerateShape();
            laserDevice = new(new Vector2(0), 100, gameObjectHandler);
            gameObjectHandler.AddAreaObject(laserDevice);
        }
        public override GameObjectHandler? GetGameObjectHandler()
        {
            return gameObjectHandler;
        }

        private void UpdateBoundaryRect(Rect gameArea)
        {
            //boundaryRect = new Rect(new Vector2(0f), game.GetSize(), new Vector2(0.5f)).ApplyMargins(0.005f, 0.005f, 0.1f, 0.005f);
            boundaryRect = gameArea.ApplyMargins(0.005f, 0.005f, 0.1f, 0.005f);
        }
        
        public override void Update(float dt, ScreenInfo game, ScreenInfo ui)
        {
            UpdateBoundaryRect(game.Area);
            gameObjectHandler.ResizeBounds(boundaryRect);
            gameObjectHandler.Update(dt, game, ui);

            for (int i = lastCutOuts.Count - 1; i >= 0; i--)
            {
                var c = lastCutOuts[i];
                c.Update(dt);
                if (c.IsFinished()) lastCutOuts.RemoveAt(i);
            }
            base.Update(dt, game, ui); //calls area update therefore area bounds have to be updated before that
        }
        private void OnAsteroidFractured(Asteroid a, Vector2 point)
        {
            var cutShape = Polygon.Generate(point, SRNG.randI(6, 12), 35, 100);
            
            FractureAsteroid(a, cutShape);
        }
        private void FractureAsteroid(Asteroid a, Polygon cutShape)
        {
            RemoveAsteroid(a);
            var asteroidShape = a.GetPolygon();
            Color color = a.GetColor();
            var fracture = fractureHelper.Fracture(asteroidShape, cutShape);
            foreach (var cutoutShape in fracture.Cutouts)
            {
                lastCutOuts.Add(new Cutout(cutoutShape));
            }
            foreach (var piece in fracture.Pieces)
            {
                Vector2 center = piece.GetCentroid();
                AsteroidShard shard = new(piece.ToPolygon(), center, color);
                gameObjectHandler.AddAreaObject(shard);
            }
            if (fracture.NewShapes.Count > 0)
            {
                foreach (var shape in fracture.NewShapes)
                {
                    float shapeArea = shape.GetArea();
                    if (shapeArea > MinPieceArea)
                    {
                        Asteroid newAsteroid = new(shape);
                        AddAsteroid(newAsteroid);
                    }
                }
            }
        }
        private void AddAsteroid(Asteroid a)
        {
            a.Fractured += OnAsteroidFractured;
            gameObjectHandler.AddAreaObject(a);
        }
        private void RemoveAsteroid(Asteroid a)
        {
            a.Fractured -= OnAsteroidFractured;
            gameObjectHandler.RemoveAreaObject(a);
        }
        private void SetCurPos(Vector2 pos)
        {
            curPos = pos;
        }
        private void CycleRotation()
        {
            float step = 45f;
            curRot += step;
            curRot = Wrap(curRot, 0f, 360f);
        }
        private void CycleSize()
        {
            float step = 50f;
            float min = 50f;
            float max = 400;
            curSize += step;
            curSize = Wrap(curSize, min, max);
        }
        private void RegenerateShape()
        {
            if (curShapeType == ShapeType.Triangle)
            {
                GenerateTriangle();
            }
            else if (curShapeType == ShapeType.Rect)
            {
                GenerateRect();
            }
            else if (curShapeType == ShapeType.Poly)
            {
                GeneratePoly();
            }
        }
        private void GenerateTriangle()
        {
            curShape = Polygon.Generate(curPos, 3, curSize / 2, curSize);
        }
        private void GenerateRect()
        {
            Rect r = new(curPos, new Vector2(curSize), new Vector2(0.5f));
            curShape = r.RotateList(new Vector2(0.5f), curRot);
        }
        private void GeneratePoly()
        {
            curShape = Polygon.Generate(curPos, 16, curSize * 0.25f, curSize);
        }
        private void PolyModeStarted()
        {
            if (curShapeType == ShapeType.None)
            {
                curShapeType = ShapeType.Triangle;
                RegenerateShape();
            }
            laserDevice.SetHybernate(true);
        }
        private void PolyModeEnded()
        {
            laserDevice.SetHybernate(false);
        }
        protected override void HandleInput(float dt, Vector2 mousePosGame, Vector2 mousePosUI)
        {
            base.HandleInput(dt, mousePosGame, mousePosUI);

            //clipRect = new(mousePosGame, new Vector2(100, 300), new Vector2(0f, 1f));
            //clipperRect = clipRect.ToClipperRect();
            //if (IsKeyPressed(KeyboardKey.KEY_NINE))
            //{
            //    Polygons newShapes = new Polygons();
            //    foreach (var shape in testShapes)
            //    {
            //        if (shape.OverlapShape(clipRect))
            //        {
            //            var result = SClipper.ClipRect(clipRect, shape, 2, false).ToPolygons(true);
            //            if (result.Count > 0) newShapes.AddRange(result);
            //        }
            //        else newShapes.Add(shape);
            //    }
            //    testShapes = newShapes;
            //}
            


            var col = gameObjectHandler.GetCollisionHandler();
            if (col == null) return;

            if (IsKeyPressed(KeyboardKey.KEY_TAB))//enter/exit poly mode
            {
                polyModeActive = !polyModeActive;
                if(polyModeActive) PolyModeStarted();
                else PolyModeEnded();
            }
            
            if (polyModeActive)
            {
                SetCurPos(mousePosGame);
                curShape.Center(curPos);

                
                var collidables = col.CastSpace(curShape, false, AsteriodLayer);
                foreach (var collidable in collidables)
                {
                    if (collidable is Asteroid asteroid)
                    {
                        asteroid.Overlapped();
                    }
                }

                if (IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) //add polygon (merge)
                {
                    Polygons polys = new();

                    if (collidables.Count > 0)
                    {
                        foreach (var collidable in collidables)
                        {
                            if (collidable is Asteroid asteroid)
                            {
                                //area.RemoveAreaObject(asteroid);
                                RemoveAsteroid(asteroid);
                                polys.Add(asteroid.GetPolygon());
                            }
                        }
                        var finalShapes = SClipper.UnionMany(curShape.ToPolygon(), polys, Clipper2Lib.FillRule.NonZero).ToPolygons(true);
                        if (finalShapes.Count > 0)
                        {
                            foreach (var f in finalShapes)
                            {
                                Asteroid a = new(f);
                                AddAsteroid(a);
                                //area.AddAreaObject(a);
                            }
                        }
                        else
                        {
                            Asteroid a = new(curShape.ToPolygon());
                            AddAsteroid(a);
                            //area.AddAreaObject(a);
                        }
                    }
                    else
                    {
                        Asteroid a = new(curShape.ToPolygon());
                        AddAsteroid(a);
                        //area.AddAreaObject(a);
                    }

                }
                if (IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT)) //cut polygon
                {
                    var cutShape = curShape.ToPolygon();
                    Polygons allCutOuts = new();
                    foreach (var collidable in collidables)
                    {
                        if (collidable is Asteroid asteroid)
                        {
                            FractureAsteroid(asteroid, cutShape);

                            /*
                            //area.RemoveAreaObject(asteroid);
                            RemoveAsteroid(asteroid);
                            var asteroidShape = asteroid.GetPolygon();

                            var fracture = fractureHelper.Fracture(asteroidShape, cutShape);

                            if (fracture.Cutouts.Count > 0) allCutOuts.AddRange(fracture.Cutouts);

                            foreach (var piece in fracture.Pieces)
                            {
                                float pieceArea = piece.GetArea();
                                //if (pieceArea < MinPieceArea) continue;

                                Vector2 center = piece.GetCentroid();
                                AsteroidShard shard = new(piece.ToPolygon(), center);
                                area.AddAreaObject(shard);
                            }
                            if(fracture.NewShapes.Count > 0)
                            {
                                foreach (var shape in fracture.NewShapes)
                                {
                                    float shapeArea = shape.GetArea();
                                    if(shapeArea > MinPieceArea)
                                    {
                                        Asteroid a = new(shape);
                                        AddAsteroid(a);
                                        //area.AddAreaObject(a);
                                    }
                                }
                            }
                            */
                        }
                    }
                    if (allCutOuts.Count > 0)
                    {
                        foreach (var cutoutShape in allCutOuts)
                        {
                            lastCutOuts.Add(new Cutout(cutoutShape));
                        }
                    }
                }

                if (IsKeyPressed(KeyboardKey.KEY_Q))//regenerate
                {
                    RegenerateShape();
                }

                if (IsKeyPressed(KeyboardKey.KEY_X))//rotate
                {
                    float oldRot = curRot;
                    CycleRotation();
                    
                    float dif = curRot - oldRot;
                    curShape.Rotate(new Vector2(0.5f), dif * DEG2RAD);
                    curShape.Center(curPos);
                }

                if (IsKeyPressed(KeyboardKey.KEY_C))//scale
                {
                    float oldSize = curSize;
                    CycleSize();
                    float scale = curSize / oldSize;
                    curShape.Scale(scale);
                    curShape.Center(curPos);
                }

                
                if (IsKeyPressed(KeyboardKey.KEY_ONE))//choose triangle
                {
                    if(curShapeType != ShapeType.Triangle)
                    {
                        GenerateTriangle();
                        curShapeType = ShapeType.Triangle;
                    }
                    
                }
                if (IsKeyPressed(KeyboardKey.KEY_TWO))//choose rectangle
                {
                    if(curShapeType != ShapeType.Rect)
                    {
                        GenerateRect();
                        curShapeType = ShapeType.Rect;
                    }
                    
                }
                if (IsKeyPressed(KeyboardKey.KEY_THREE))//choose polygon 
                {
                    if (curShapeType != ShapeType.Poly)
                    {
                        GeneratePoly();
                        curShapeType = ShapeType.Poly;
                    }
                    
                }
            }
            else
            {
                if(IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    laserDevice.SetAimingMode(false);
                }
                else if (IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    laserDevice.SetAimingMode(true);
                }
            }
        }



        public override void DrawGame(ScreenInfo game)
        {
            base.DrawGame(game);
            
            boundaryRect.DrawLines(4f, ColorLight);
            if(polyModeActive && curShapeType != ShapeType.None)
            {
                curShape.DrawLines(2f, RED);
            }

            gameObjectHandler.DrawGame(game);

            foreach (var cutOut in lastCutOuts)
            {
                cutOut.Draw();
            }
            
            //var ellipse = SClipper.CreateEllipse(mousePosGame, 500, 100, 0);
            //ellipse.DEBUG_DrawLinesCCW(2f, BLUE, PURPLE);
            //foreach (var shape in testShapes)
            //{
            //    shape.DEBUG_DrawLinesCCW(2f, BLUE, PURPLE);
            //}
            //clipRect.DrawLines(8f, RED);
            //
            //var conversionRect = clipperRect.ToRect();
            //conversionRect.DrawLines(4f, YELLOW);
            //
            //Polygon clipperPolygon = new();
            //clipperPolygon.Add(new Vector2((float)clipperRect.left, (float)clipperRect.top));
            //clipperPolygon.Add(new Vector2((float)clipperRect.left, (float)clipperRect.bottom));
            //clipperPolygon.Add(new Vector2((float)clipperRect.right, (float)clipperRect.bottom));
            //clipperPolygon.Add(new Vector2((float)clipperRect.right, (float)clipperRect.top));
            //clipperPolygon.DrawLines(2f, GREEN);
        }
        public override void DrawUI(ScreenInfo ui)
        {
            gameObjectHandler.DrawUI(ui);
            base.DrawUI(ui);
            Vector2 uiSize = ui.Area.Size;
            Rect infoRect = new Rect(uiSize * new Vector2(0.5f, 0.99f), uiSize * new Vector2(0.95f, 0.07f), new Vector2(0.5f, 1f));

            string polymodeText = "[Tab] Polymode | [LMB] Place/Merge | [RMB] Cut | [1] Triangle | [2] Rect | [3] Poly | [Q] Regenerate | [X] Rotate | [C] Scale";
            string laserText = "[Tab] Lasermode | [LMB] Move | [RMB] Shoot Laser";
            string text = polyModeActive ? polymodeText : laserText;
            //string infoText = String.Format("Object Count: {0}", area.Count); // MathF.Floor(crossResult * 100) / 100);

            //text = String.Format("RPos: {0} | CRPos: {1}", new Vector2(clipRect.x, clipRect.y), new Vector2((float)clipperRect.left, (float)clipperRect.top));
            
            font.DrawText(text, infoRect, 1f, new Vector2(0.5f, 0.5f), ColorLight);
            
            
        }

    }
}
