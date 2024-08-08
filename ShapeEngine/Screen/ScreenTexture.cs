using System.Data;
using System.Numerics;
using Raylib_cs;
using ShapeEngine.Color;
using ShapeEngine.Core.Shapes;
using ShapeEngine.Core.Structs;
using ShapeEngine.Lib;

namespace ShapeEngine.Screen;


public sealed class ScreenTexture2
{
    public enum ScreenTextureMode
    {
        Stretch = 1,
        Pixelation = 2,
        Fixed = 4
    }

    public enum ShaderSupportType
    {
        None = 1,
        Single = 2,
        Multi = 4
    }
    
    public readonly Dimensions FixedDimensions;
    public readonly float PixelationFactor;
    public readonly ScreenTextureMode Mode;
    public readonly TextureFilter TextureFilter;
    public readonly ShaderSupportType ShaderSupport;
    public readonly ShaderContainer? Shaders = null;
    private RenderTexture2D ShaderBuffer = new();
    private ShapeShader? SingleShader = null;
    private Rect textureRect = new();
    private Dimensions ScreenDimensions = new();
    
    public ScreenInfo ScreenInfo { get; private set; } = new();
    public bool Loaded { get; private set; } = false;
    public RenderTexture2D RenderTexture { get; private set; } = new();
    public int Width { get; private set; } = 0;
    public int Height { get; private set; } = 0;
    public ShapeCamera? Camera = null;
    public ColorRgba Tint = new ColorRgba(1, 1, 1, 1);

    
    public ScreenTexture2(ShaderSupportType shaderSupportType, TextureFilter textureFilter = TextureFilter.Bilinear)
    {
        TextureFilter = textureFilter;
        ShaderSupport = shaderSupportType;
        if (shaderSupportType != ShaderSupportType.None)
        {
            Shaders = new();
        }
        FixedDimensions = new Dimensions();
        PixelationFactor = 1f;
        Mode = ScreenTextureMode.Stretch;
    }
    public ScreenTexture2(float pixelationFactor, ShaderSupportType shaderSupportType, TextureFilter textureFilter = TextureFilter.Bilinear)
    {
        TextureFilter = textureFilter;
        ShaderSupport = shaderSupportType;
        if (shaderSupportType != ShaderSupportType.None)
        {
            Shaders = new();
        }
        
        FixedDimensions = new Dimensions();
        PixelationFactor = pixelationFactor;
        if (pixelationFactor is <= 0f or > 1f)
        {
            PixelationFactor = 1f;
            Mode = ScreenTextureMode.Stretch;
        }
        else Mode = ScreenTextureMode.Pixelation;
    }
    public ScreenTexture2(Dimensions fixedDimensions, ShaderSupportType shaderSupportType, TextureFilter textureFilter = TextureFilter.Bilinear)
    {
        TextureFilter = textureFilter;
        ShaderSupport = shaderSupportType;
        if (shaderSupportType != ShaderSupportType.None)
        {
            Shaders = new();
        }
        
        FixedDimensions = fixedDimensions;
        PixelationFactor = 1f;
        if (!fixedDimensions.IsValid())
        {
            FixedDimensions = new();
            Mode = ScreenTextureMode.Stretch;
        }
        else
        {
            Loaded = true;
            Mode = ScreenTextureMode.Fixed;
            Width = FixedDimensions.Width;
            Height = FixedDimensions.Height;
            
            RenderTexture = Raylib.LoadRenderTexture(Width, Height);
            Raylib.SetTextureFilter(RenderTexture.Texture, TextureFilter);

            if (shaderSupportType != ShaderSupportType.None)
            {
                ShaderBuffer = Raylib.LoadRenderTexture(Width, Height);
                Raylib.SetTextureFilter(ShaderBuffer.Texture, TextureFilter);
            }
            
        }
    }

    
    public void Update(float dt, Dimensions screenSize, Vector2 mousePosition, bool paused)
    {
        ScreenDimensions = screenSize;
        UpdateTexture(screenSize);
        textureRect = CalculateTextureRect();
        if (Camera != null)
        {
            Camera.SetSize(new Dimensions(Width, Height), new Dimensions(Width, Height));
            if(!paused) Camera.Update(dt);
        }

        var scaledMousePosition = mousePosition;
        if (Camera != null)
        {
            scaledMousePosition = Camera.ScreenToWorld(mousePosition);
        }
        else
        {
            if (Mode == ScreenTextureMode.Pixelation)
            {
                scaledMousePosition = mousePosition / PixelationFactor;
            }
            else if (Mode == ScreenTextureMode.Fixed)
            {
                if (textureRect.X > 0)
                {
                    var f = screenSize.Height / textureRect.Height;
                    float xOffset = textureRect.X;
                    float mouseX = (mousePosition.X - xOffset) / f;
                    scaledMousePosition = new Vector2(mouseX, mousePosition.Y / f);
                }
                else if (textureRect.Y > 0)
                {
                    var f = screenSize.Width / textureRect.Width;
                    float yOffset = textureRect.Y;
                    float mouseY = (mousePosition.Y - yOffset) / f;
                    scaledMousePosition = new Vector2(mousePosition.X / f, mouseY);
                }
                else
                {
                    var f = screenSize.Width / textureRect.Width;
                    scaledMousePosition = mousePosition / f;
                }
            }
        }
        
        var area = Camera?.Area ?? textureRect;
        
        ScreenInfo = new(area, scaledMousePosition);
    }
    
    public void Draw()
    {
        bool shaderMode = ShaderSupport != ShaderSupportType.None && Shaders != null && Shaders.HasActiveShaders();

        if (shaderMode)
        {
            Raylib.BeginTextureMode(RenderTexture);
            Raylib.ClearBackground(new(0,0,0,0));

            if (Camera != null)
            {
                Raylib.BeginMode2D(Camera.Camera);
                //DRAW GAME
                Raylib.EndMode2D();
            }
            else
            {
                //DRAW GAME
            }
            
            Raylib.EndTextureMode();
            
            ApplyShaders(); // we know that we have shaders active
            
            Raylib.BeginTextureMode(RenderTexture);
            // DRAW UI
            Raylib.EndTextureMode();
            
            DrawToScreen();
        }
        else
        {
            Raylib.BeginTextureMode(RenderTexture);
            Raylib.ClearBackground(new(0,0,0,0));

            if (Camera != null)
            {
                Raylib.BeginMode2D(Camera.Camera);
                //DRAW GAME
                Raylib.EndMode2D();
            }
            else
            {
                //DRAW GAME
            }
            //DRAW UI
            
            Raylib.EndTextureMode();
            
            DrawToScreen();
            
        }
    }
    
    public void Unload()
    {
        if (!Loaded) return;
        Loaded = false;
        Raylib.UnloadRenderTexture(RenderTexture);
        if(ShaderSupport != ShaderSupportType.None) Raylib.UnloadRenderTexture(ShaderBuffer);
    }

    
    private Rect CalculateTextureRect()
    {
        if(Mode is ScreenTextureMode.Stretch or ScreenTextureMode.Pixelation) return new Rect(0, 0, Width, Height);

        var wDif = ShapeMath.AbsInt(ScreenDimensions.Width - Width);
        var hDif = ShapeMath.AbsInt(ScreenDimensions.Height - Height);
        if (wDif == hDif)
        {
            return new Rect(0, 0, Width, Height);
        }
        
        if (wDif > hDif)
        {
            //scale to height
            float hFactor = (float)ScreenDimensions.Height / Height;
            var newW = (int)(Width * hFactor);
            return new Rect((ScreenDimensions.Width - newW) / 2f, 0f, newW, Height);
            
        }
        
        //scale to width
        float wFactor = (float)ScreenDimensions.Width / Width;
        var newH = (int)(Height * wFactor);
        return new Rect((ScreenDimensions.Width - newH) / 2f, 0f, Width, newH);
    }
    private void DrawToScreen()
    {
        if (SingleShader != null)
        {
            Raylib.BeginShaderMode(SingleShader.Shader);
            DrawTextureToScreen(RenderTexture.Texture);
            Raylib.EndShaderMode();
        }
        else
        {
            DrawTextureToScreen(RenderTexture.Texture);
        }
        
        
    }
    private void ApplyShaders()
    {
        if (ShaderSupport == ShaderSupportType.None) return;
        if (Shaders == null) return;
        
        var activeScreenShaders = Shaders.GetActiveShaders();
        if (activeScreenShaders.Count <= 0) return;

        if (activeScreenShaders.Count == 1)
        {
            SingleShader = activeScreenShaders[0];
            return;
        }

        bool evenCount = activeScreenShaders.Count % 2 == 0;
        
        RenderTexture2D source = RenderTexture;
        RenderTexture2D target = ShaderBuffer;
        RenderTexture2D temp;
        
        foreach (var shader in activeScreenShaders)
        {
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(new(0,0,0,0));
            Raylib.BeginShaderMode(shader.Shader);
            ApplyShaderTexture(source.Texture);//draws texture to target
            Raylib.EndShaderMode();
            Raylib.EndTextureMode();
            temp = source;
            source = target;
            target = temp;
        }

        if (evenCount)
        {
            RenderTexture = source;
            ShaderBuffer = target;
        }
        else
        {
            RenderTexture = target;
            ShaderBuffer = source;
        }
    }
    private void ApplyShaderTexture(Texture2D texture)
    {
        var destRec = new Rectangle
        {
            X = Width * 0.5f,
            Y = Height * 0.5f,
            Width = Width,
            Height = Height
        };
        Vector2 origin = new()
        {
            X = Width * 0.5f,
            Y = Height * 0.5f
        };
        
        var sourceRec = new Rectangle(0, 0, Width, -Height);
        
        Raylib.DrawTexturePro(texture, sourceRec, destRec, origin, 0f, new ColorRgba(System.Drawing.Color.White).ToRayColor());
    }
    private void UpdateTexture(Dimensions screenSize)
    {
        if (Mode == ScreenTextureMode.Fixed) return;

        int w = screenSize.Width;
        int h = screenSize.Height;
        
        if (Mode == ScreenTextureMode.Pixelation)
        {
            w = (int)(w * PixelationFactor);
            h = (int)(h * PixelationFactor);
        }
        
        ReloadTexture(w, h);
    }
    private void ReloadTexture(int w, int h)
    {
        if (!Loaded) Loaded = true;
        
        Width = w;
        Height = h;
        
        if (ShaderSupport == ShaderSupportType.None)
        {
            Raylib.UnloadRenderTexture(RenderTexture);
                
            RenderTexture = Raylib.LoadRenderTexture(Width, Height);
            Raylib.SetTextureFilter(RenderTexture.Texture, TextureFilter);
        }
        else
        {
            Raylib.UnloadRenderTexture(RenderTexture);
            Raylib.UnloadRenderTexture(ShaderBuffer);
            
            RenderTexture = Raylib.LoadRenderTexture(Width, Height);
            Raylib.SetTextureFilter(RenderTexture.Texture, TextureFilter);
                
            ShaderBuffer = Raylib.LoadRenderTexture(Width, Height);
            Raylib.SetTextureFilter(ShaderBuffer.Texture, TextureFilter);
        }
    }
    private void DrawTextureToScreen(Texture2D texture)
    {
        if (Mode == ScreenTextureMode.Stretch)
        {
            var w = Width;
            var h = Height;
            var destRec = new Rectangle
            {
                X = w * 0.5f,
                Y = h * 0.5f,
                Width = w,
                Height = h
            };
            Vector2 origin = new()
            {
                X = w * 0.5f,
                Y = h * 0.5f
            };
        
            var sourceRec = new Rectangle(0, 0, w, -h);
        
            Raylib.DrawTexturePro(texture, sourceRec, destRec, origin, 0f, Tint.ToRayColor());
        }
        else if (Mode == ScreenTextureMode.Pixelation)
        {
            var w = ScreenDimensions.Width;
            var h = ScreenDimensions.Height;
            var destRec = new Rectangle
            {
                X = w * 0.5f,
                Y = h * 0.5f,
                Width = w,
                Height = h
            };
            Vector2 origin = new()
            {
                X = w * 0.5f,
                Y = h * 0.5f
            };
        
            var sourceRec = new Rectangle(0, 0, Width, -Height);
        
            Raylib.DrawTexturePro(texture, sourceRec, destRec, origin, 0f, Tint.ToRayColor());
        }
        else // Fixed Mode
        {
            var w = ScreenDimensions.Width;
            var h = ScreenDimensions.Height;
            var destRec = new Rectangle
            {
                X = w * 0.5f,
                Y = h * 0.5f,
                Width = w,
                Height = h
            };
            Vector2 origin = new()
            {
                X = w * 0.5f,
                Y = h * 0.5f
            };
        
            var sourceRec = new Rectangle(0, 0, Width, -Height);
        
            Raylib.DrawTexturePro(texture, sourceRec, destRec, origin, 0f, Tint.ToRayColor());
        }
        
    }
    
}

internal sealed class ScreenTexture
{
    public bool Loaded { get; private set; } = false;
    public RenderTexture2D RenderTexture { get; private set; } = new();
    public int Width { get; private set; } = 0;
    public int Height { get; private set; } = 0;
    
    
    /// <summary>
    /// Requires to unload and load the texture to take effect!
    /// </summary>
    public TextureFilter TextureFilter = TextureFilter.Bilinear;

    public ScreenTexture(){}

    public void Load(Dimensions dimensions)
    {
        if (Loaded) return;
        Loaded = true;
        SetTexture(dimensions);
    }
    public void Unload()
    {
        if (!Loaded) return;
        Loaded = false;
        Raylib.UnloadRenderTexture(RenderTexture);
    }
    public void UpdateDimensions(Dimensions dimensions)
    {
        if (!Loaded) return;

        if (Width == dimensions.Width && Height == dimensions.Height) return;
        
        Raylib.UnloadRenderTexture(RenderTexture);
        SetTexture(dimensions);
    }
    public void Draw()
    {
        var destRec = new Rectangle
        {
            X = Width * 0.5f,
            Y = Height * 0.5f,
            Width = Width,
            Height = Height
        };
        Vector2 origin = new()
        {
            X = Width * 0.5f,
            Y = Height * 0.5f
        };
        
        var sourceRec = new Rectangle(0, 0, Width, -Height);
        
        Raylib.DrawTexturePro(RenderTexture.Texture, sourceRec, destRec, origin, 0f, new ColorRgba(System.Drawing.Color.White).ToRayColor());
    }
    
    private void SetTexture(Dimensions dimensions)
    {
        Width = dimensions.Width;
        Height = dimensions.Height;
        RenderTexture = Raylib.LoadRenderTexture(Width, Height);
        Raylib.SetTextureFilter(RenderTexture.Texture, TextureFilter);
    }
    
    //public void DrawTexture(int targetWidth, int targetHeight)
    //{
    //var destRec = new Rectangle
    //{
    //    x = targetWidth * 0.5f,
    //    y = targetHeight * 0.5f,
    //    width = targetWidth,
    //    height = targetHeight
    //};
    //Vector2 origin = new()
    //{
    //    X = targetWidth * 0.5f,
    //    Y = targetHeight * 0.5f
    //};
    //
    //
    //
    //var sourceRec = new Rectangle(0, 0, Width, -Height);
    //
    //DrawTexturePro(RenderTexture.texture, sourceRec, destRec, origin, 0f, WHITE);
    //
}