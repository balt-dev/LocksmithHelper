using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.Utils;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Celeste.Mod.LocksmithHelper.Entities;

[CustomEntity("LocksmithHelper/Door", "LockpickDoor")]
[Tracked]
public class Door : Solid {
    public struct Requirement {
        public Complex? Value;
        public LockColor Color;
        public Rectangle? DrawBox;

        public readonly Complex? Cost(Complex copyMult, bool force = false) {
            var invCount = Key.Inventory[Color].Count;
            if (Value == null) return invCount == 0 ? 0 : null;
            var value = (Complex) Value;
            if (invCount == 0 && !force) return null;
            #pragma warning disable CS1718
            if (value != value) // NaN == All
                return invCount;
            #pragma warning restore CS1718
            if (double.IsInfinity(value.Real))
                return SameSignAndAbsGeq(invCount.Real, double.Sign(value.Real)) || force
                    ? new Complex(invCount.Real, 0) * copyMult
                    : null;
            else if (double.IsInfinity(value.Imaginary))
                return SameSignAndAbsGeq(invCount.Imaginary, double.Sign(value.Imaginary)) || force
                    ? new Complex(0, invCount.Imaginary) * copyMult
                    : null;
            Complex cost = value * copyMult;
            return (SameSignAndAbsGeq(invCount.Real, cost.Real)
                && SameSignAndAbsGeq(invCount.Imaginary, cost.Imaginary))
                || force
                ? cost
                : null;
        }

        private static bool SameSignAndAbsGeq(double a, double b) {
            return b == 0 || (a != 0 && Math.Sign(a) == Math.Sign(b) && Math.Abs(a) >= Math.Abs(b));
        }
    }

    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        for (var i = 0; i < _requirements.Count; i++) {
            var req = GetRequirement(i);
            var center = (
                req.DrawBox ?? new Rectangle(6, 6, (int) Width - 12, (int) Height - 12)
            ).Center.ToVector2();
            DrawComplex(req.Value ?? 0, Position + center - Vector2.UnitY * 6, Color.Blue);
            DrawComplex((Complex) req.Cost(CopyMult, true), Position + center, req.Cost(CopyMult) != null ? Color.Green : Color.Red);
        }
    }

    public static LockColor? LastSpentColor;

    private readonly LockColor _spend;
    public LockColor Spend { get {
        if (Cursed) return LockColor.Brown;
        if (_spend != LockColor.Glitch) return _spend;
        return LastSpentColor ?? LockColor.Glitch;
    }}
    private readonly List<Requirement> _requirements;
    private readonly Complex _visualRequirementSum;

    private static readonly MTexture doorAtlas = GFX.Game["objects/LocksmithHelper/door/atlas"];
    private static readonly MTexture[,] doorOutline = new MTexture[3, 3];
    private static readonly MTexture[,] doorInline = new MTexture[3, 3];
    private static readonly MTexture[,] doorOutlineInverse = new MTexture[3, 3];
    private static readonly MTexture[,] doorInlineInverse = new MTexture[3, 3];
    private static readonly MTexture Keyhole = new(doorAtlas, new(12, 10, 4, 6));
    private static readonly MTexture Blast = new(doorAtlas, new(22, 10, 6, 6));
    private static readonly MTexture All = new(doorAtlas, new(16, 10, 6, 6));
    private static readonly MTexture[] Numbers = new MTexture[10];
    private static readonly MTexture Plus = new(doorAtlas, new(33, 0, 3, 5));
    private static readonly MTexture Minus = new(doorAtlas, new(36, 0, 3, 5));
    private static readonly MTexture I = new(doorAtlas, new(33, 5, 3, 5));
    private static readonly MTexture Times = new(doorAtlas, new(36, 5, 3, 5));
    private static readonly MTexture FrozenTL = GFX.Game["objects/LocksmithHelper/door/frozen_tl"];
    private static readonly MTexture FrozenBR = GFX.Game["objects/LocksmithHelper/door/frozen_br"];
    private static readonly MTexture Erosion = GFX.Game["objects/LocksmithHelper/door/erosion"];
    private static readonly MTexture Paint = GFX.Game["objects/LocksmithHelper/door/paint"];
   

    static Door() {
        for (var idx = 0; idx < 9; idx++) {
            doorOutline[idx / 3, idx % 3] = new(doorAtlas, new(idx % 3 * 3, idx / 3 * 3, 3, 3));
            doorInline[idx / 3, idx % 3] = new(doorAtlas, new(idx % 3 * 2, idx / 3 * 2 + 9, 2, 2));
            doorOutlineInverse[idx / 3, idx % 3] = new(doorAtlas, new(idx % 3 * 3 + 9, idx / 3 * 3, 3, 3));
            doorInlineInverse[idx / 3, idx % 3] = new(doorAtlas, new(idx % 3 * 2 + 6, idx / 3 * 2 + 9, 2, 2));
        }
        for (var n = 0; n < 10; n++)
            Numbers[n] = new(doorAtlas, new(18 + n % 5 * 3, n / 5 * 5, 3, 5));
    }


    public static readonly ParticleType CurseParticle = new() {
        Color = LockColor.Brown.ForceToColor() * 0.3f,
        FadeMode = ParticleType.FadeModes.Late,
        LifeMax = 2.5f,
        LifeMin = 1.3f,
        Size = 5,
        Direction = (float) Math.PI * 3 / 2,
        SpeedMin = 0.8f,
        SpeedMax = 1.4f
    };

    private static readonly ParticleType BreakParticle = new() {
        FadeMode = ParticleType.FadeModes.Late,
        LifeMax = 0.5f,
        LifeMin = 0.3f,
        Size = 8,
        Direction = (float) Math.PI * 1 / 2,
        SpeedMin = 6f,
        SpeedMax = 10f
    };


    public Requirement GetRequirement(int index) {
        var actual = _requirements[index];
        // NOTE: Depends on the by-value nature of structs. Do not make Requirement a class.
        if (Cursed) actual.Color = LockColor.Brown;
        return actual;
    }

    public Complex Copies {get; private set;}

    private bool _cursed;
    public bool Cursed {
        get => _cursed;
        set => _cursed = Spend != LockColor.Pure && value;
    }

    public bool Eroded;
    public bool Frozen;
    public bool Painted;

    private float Cooldown;

    public Door(EntityData data, Vector2 offset)
        : base(data.Position + offset, data.Width, data.Height, false)
    {
        Safe = false;
        Eroded = data.Bool("eroded");
        Frozen = data.Bool("frozen");
        Painted = data.Bool("painted");
        Cursed = data.Bool("cursed");
        Copies = ComplexExt.Parse(data.Attr("copies", "1"));
        _requirements = [];
        _visualRequirementSum = Complex.Zero;

        _spend = data.Enum("spend", LockColor.Orange);
        var requirementString = data.Attr("requirements", "Orange: 1");
        // Format: "Color: Value"
        // Format: "Color: Value @ X Y W H"
        foreach (var requirement in requirementString.Split(",")) {
            if (requirement.Length == 0)
                continue;
            var colonIndex = requirement.IndexOf(':');
            if (colonIndex < 0) throw new Exception($"Malformed requirement string \"{requirementString}\"");
            var reqColorString = requirement[..colonIndex].Trim();
            var reqColor = Enum.Parse<LockColor>(reqColorString);
            var reqValueString = requirement[(colonIndex + 1)..].Trim();
            Rectangle? reqBox = null;
            var atIndex = reqValueString.IndexOf('@');
            if (atIndex > -1) {
                int[] box = reqValueString[(atIndex + 1)..]
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToArray();
                if (box.Length != 4)
                    throw new Exception($"Malformed box dimensions in requirement string \"{requirement}\" (must have exactly 4 elements)");
                reqValueString = reqValueString[..atIndex].Trim();
                reqBox = new Rectangle(box[0], box[1], box[2], box[3]);
            }
            Complex? reqValue;
            var blast = false;
            if (reqValueString == "blank") {
                reqValue = null;
                goto End;
            }
            if (reqValueString == "all") {
                reqValue = Complex.NaN;
                goto End;
            }
            if (reqValueString.EndsWith("x")) {
                reqValueString = reqValueString.TrimEnd('x');
                blast = true;
            }
            try {
                reqValue = ComplexExt.Parse(reqValueString);
            } catch (FormatException exception) {
                throw new Exception($"Failed to parse \"{reqValueString}\" as a complex number in requirement string \"{requirementString}\"", exception);
            }
            if (blast)
                if (reqValue == Complex.One)
                    reqValue = new Complex(float.PositiveInfinity, 0);
                else if (reqValue == Complex.ImaginaryOne)
                    reqValue = new Complex(0, float.PositiveInfinity);
                else if (reqValue == -Complex.One)
                    reqValue = new Complex(float.NegativeInfinity, 0);
                else if (reqValue == -Complex.ImaginaryOne)
                    reqValue = new Complex(0, float.NegativeInfinity);
                else
                    throw new Exception($"Blast door must have unit vector as value in \"{requirement}\"");
            _visualRequirementSum += reqValue ?? 0;
        End:
            _requirements.Add(new Requirement { Value = reqValue, Color = reqColor, DrawBox = reqBox});
        }

        Add(new PlayerCollider(AuraCollide, new Hitbox(Width + 32, Height + 32, -16, -16)));
        OnCollide = TryOpen;
    }

    private bool AnyIsColor(LockColor color) {
        if (_spend == color)
            return true;
        for (int i = 0; i < _requirements.Count; i++)
            if (GetRequirement(i).Color == color)
                return true;
        return false;
    }

    // TODO: Clean this code up, it sucks.
    private void AuraCollide(Player player) {
        if (Frozen && Key.Inventory[LockColor.Red].Count.Real >= 1) {
            Frozen = false;
            Audio.Play("event:/game/09_core/iceball_break");
            Cooldown = 0.05f;
        }
        if (Eroded && Key.Inventory[LockColor.Green].Count.Real >= 5) {
            Eroded = false;
            Audio.Play("event:/game/04_cliffside/arrowblock_reappear");
            Cooldown = 0.05f;
        }
        if (Painted && Key.Inventory[LockColor.Blue].Count.Real >= 3) {
            Painted = false;
            Audio.Play("event:/game/04_cliffside/snowball_impact");
            Cooldown = 0.05f;
        }
        if (!Cursed && !AnyIsColor(LockColor.Brown) && Key.Inventory[LockColor.Brown].Count.Real > 0) {
            Cursed = true;
            Audio.Play("event:/game/03_resort/fallblock_wood_shake");
            Cooldown = 0.05f;
        }
        if (Cursed && Key.Inventory[LockColor.Brown].Count.Real < 0) {
            Cursed = false;
            Audio.Play("event:/game/03_resort/fallblock_wood_impact");
            Cooldown = 0.05f;
        }
    }

    private static Complex ViewMult => LocksmithHelperModule.ImaginaryView ? Complex.ImaginaryOne : Complex.One; 

    public void TryOpen(Vector2 pos) {
        if (Cooldown > 0) return;
        if (Eroded || Painted || Frozen) return;

        var oldCopies = Copies;
        if (LocksmithHelperModule.MasterKeyReady && !AnyIsColor(LockColor.Master) && !AnyIsColor(LockColor.Pure) && Key.Inventory[LockColor.Master].Count.RealWithView() != 0) {
            var rotatedCount = Key.Inventory[LockColor.Master].Count * ViewMult;
            var spentKeys = new Complex(Math.Sign(rotatedCount.Real), 0) / ViewMult;
            Copies -= spentKeys;

            if (!Key.Inventory[LockColor.Master].Locked)
                Key.Inventory[LockColor.Master].Count -= spentKeys;
            LocksmithHelperModule._masterReady = false;
            goto End;
        }

        if (Copies.Real == 0 || !TrySpend(false))
            if (Copies.Imaginary == 0 || !TrySpend(true))
                return;

        End:
        if (oldCopies != Copies) {
            Audio.Play("event:/game/general/wall_break_stone");
            if (Copies == 0) {
                LastSpentColor = Spend;
                RemoveSelf();
                BreakParticle.Color = Spend.ForceToColor();
                for (var x = 0; x < Width; x += 8)
                    for (var y = 0; y < Height; y += 8)
                        (Scene as Level).ParticlesFG.Emit(BreakParticle, Position + new Vector2(x + 4, y + 4));
                Visible = false;
                return;
            }
            Cooldown = 0.3f;
        }
    }

    private bool TrySpend(bool imag) {
        var totalCost = Complex.Zero;
        for (int i = 0; i < _requirements.Count; i++) {
            var req = GetRequirement(i);
            Complex copyMul = imag ? new(0, CopyMult.Imaginary) : new(CopyMult.Real, 0);
            var cost = req.Cost(copyMul);
            if (cost == null) return false;

            totalCost += (Complex) cost;
        }
        
        if (!Key.Inventory[Spend].Locked)
            Key.Inventory[Spend].Count -= totalCost;
        if (imag) {
            if (Copies.Imaginary > 0)
                Copies = new(Copies.Real, Copies.Imaginary - 1);
            else
                Copies = new(Copies.Real, Copies.Imaginary + 1);
        } else {
            if (Copies.Real > 0)
                    Copies = new(Copies.Real - 1, Copies.Imaginary);
                else
                    Copies = new(Copies.Real + 1, Copies.Imaginary);
        }
        
        return true;
    }

    #region Rendering

    private readonly List<Tuple<float, float, float, float, LockColor>> DeferredRectangles = [];

    public override void Update()
    {
        base.Update();
        if (Cooldown > 0)
            Cooldown -= Engine.DeltaTime;
        if (Cursed && Scene.OnInterval(0.2f))
            for (var i = 0; i < 5; i++)
                (Scene as Level).ParticlesFG.Emit(
                    CurseParticle,
                    TopLeft + new Vector2((Width + 12) * Calc.Random.NextFloat() - 6, (Height + 12) * Calc.Random.NextFloat() - 6)
                );
        
    }

    private Complex CopyMult => new(Math.Sign(Copies.Real), Math.Sign(Copies.Imaginary));
    private double VisualCopyUnit => Math.Sign(Copies.RealWithView());

    public override void Render() {
        DeferredRectangles.Clear();
        RenderColor(X, Y, Width, Height, Cursed ? LockColor.Brown : _spend);

        for (int i = 0; i < _requirements.Count; i++) {
            var req = _requirements[i];
            if (Cursed)
                req.Color = LockColor.Brown;
            Rectangle rect = req.DrawBox ?? new Rectangle(6, 6, (int) Width - 12, (int) Height - 12);
            RenderColor(X + rect.X, Y + rect.Y, rect.Width, rect.Height, req.Color);
        }

        DrawDeferredRects();
        
        bool? anySigils = null;
        Complex requirementSum = 0;
        foreach (var req in _requirements) {
            anySigils ??= false;
            Rectangle rect = req.DrawBox ?? new Rectangle(6, 6, (int) Width - 12, (int) Height - 12);
            var displayValue = ((req.Value ?? 0) * CopyMult).RealWithView();
            DrawSlices(displayValue < 0 ? doorInlineInverse : doorInline, X + rect.X, Y + rect.Y, rect.Width, rect.Height);
            if (req.Value == null) {
                anySigils = true;
                continue;
            }
            var value = (Complex) req.Value;
            if ((!double.IsNaN(displayValue) || (double.IsNaN(value.Real) && double.IsNaN(value.Real))) && displayValue != 0) {
                requirementSum += double.IsFinite(value.Real)
                    && double.IsFinite(value.Imaginary)
                    ? value
                    : Complex.IsNaN(value) ? 0 : new Complex(Math.Sign(value.Real), Math.Sign(value.Imaginary));
                anySigils |= RenderSigil(Position + rect.Center.ToVector2(), value * CopyMult);
            }
        }
        
        DrawSlices(
            (requirementSum * CopyMult).RealWithView() < 0 || (anySigils == false) ? doorOutlineInverse : doorOutline,
            X, Y, Width, Height,
            anySigils == false ? ColorExt.ColorFromHSV(Scene.TimeActive / 8, 1, 1) : Color.White
        );

        if (Eroded)
            for (var x = 0; x < Width; x += Erosion.Width)
                for (var y = 0; y < Height; y += Erosion.Height)
                    Erosion.Draw(
                        TopLeft + new Vector2(x, y), Vector2.Zero, Color.White, Vector2.One, 0,
                        new Rectangle(
                            0, 0, 
                            (int) (x + Erosion.Width > Width ? Width - x : Erosion.Width), 
                            (int) (y + Erosion.Height > Height ? Height - y : Erosion.Height)
                        )
                    );
        
        if (Painted)
            for (var x = 0; x < Width; x += Paint.Width)
                for (var y = 0; y < Height; y += Paint.Height)
                    Paint.Draw(
                        TopLeft + new Vector2(x, y), Vector2.Zero, Color.White, Vector2.One, 0,
                        new Rectangle(
                            0, 0, 
                            (int) (x + Paint.Width > Width ? Width - x : Paint.Width), 
                            (int) (y + Paint.Height > Height ? Height - y : Paint.Height)
                        )
                    );

        if (Frozen) {
            Draw.Rect(Collider, Color.White * 0.2f);
            Draw.HollowRect(Collider, Color.White * 0.5f);
            FrozenTL.Draw(
                TopLeft + Collider.TopLeft, Vector2.Zero, Color.White, Vector2.One, 0,
                new Rectangle(0, 0, (int) Width, (int) Height)
            );
            FrozenBR.Draw(
                TopLeft + Collider.BottomRight, new Vector2(Math.Min(Width, FrozenBR.Width), Math.Min(Height, FrozenBR.Height)), Color.White, Vector2.One, 0,
                new Rectangle(Math.Max(0, FrozenBR.Width - (int) Width), Math.Max(0, FrozenBR.Height - (int) Height), FrozenBR.Width, FrozenBR.Height)
            );
        }
        
        if (Cooldown > 0)
            Draw.Rect(Collider, Color.White * (float) Math.Sqrt(Cooldown * 20));

        if (Copies != 1)
            DrawComplex(Copies, TopCenter - new Vector2(0, 4), Color.White, true, true);
    }

    private static void DrawSlices(MTexture[,] slices, float x, float y, float width, float height, Color? color = null)
    {
        var drawColor = color ?? Color.White;
        var borderWidth = slices[0, 0].Width;
        var borderHeight = slices[0, 0].Height;
        var insideWidth = slices[1, 1].Width;
        var insideHeight = slices[1, 1].Height;
        var internalWidth = width - borderWidth * 2;
        var internalHeight = height - borderHeight * 2;

        slices[0, 0].Draw(new(x, y), Vector2.Zero, drawColor);
        slices[0, 1].Draw(new(x + borderWidth, y), Vector2.Zero, drawColor, new Vector2(internalWidth / insideWidth, 1));
        slices[0, 2].Draw(new(x + borderWidth + internalWidth, y), Vector2.Zero, drawColor);
        slices[1, 0].Draw(new(x, y + borderHeight), Vector2.Zero, drawColor, new Vector2(1, internalHeight / insideHeight));
        slices[1, 1].Draw(new(x + borderWidth, y + borderHeight), Vector2.Zero, drawColor, new Vector2(internalWidth / insideWidth, internalHeight / insideHeight));
        slices[1, 2].Draw(new(x + borderWidth + internalWidth, y + borderHeight), Vector2.Zero, drawColor, new Vector2(1, internalHeight / insideHeight));
        slices[2, 0].Draw(new(x, y + borderHeight + internalHeight), Vector2.Zero, drawColor);
        slices[2, 1].Draw(new(x + borderWidth, y + borderHeight + internalHeight), Vector2.Zero, drawColor, new Vector2(internalWidth / insideWidth, 1));
        slices[2, 2].Draw(new(x + borderWidth + internalWidth, y + borderHeight + internalHeight), Vector2.Zero, drawColor);
    }

    public static void DrawComplex(Complex value, Vector2 center, Color color, bool outline = false, bool mult = false, bool centered = true) {
        if (double.IsInfinity(value.Real))
            value = new(double.CopySign(808f, value.Real), value.Imaginary);
        if (double.IsInfinity(value.Imaginary))
            value = new(value.Real, double.CopySign(808f, value.Imaginary));
        if (value != value)
            value = new(12345, 54321);
        var valueStr = value.AsString();
        if (mult)
            valueStr = "x" + valueStr;
        var width = valueStr.Length * 4 - 1;
        var x = centered ? center.X - (float) width / 2 + 1 : center.X;

        foreach (char chr in valueStr) {
            MTexture sprite;
            if (char.IsAsciiDigit(chr))
                sprite = Numbers[chr - '0'];
            else
                sprite = chr switch {
                    '+' => Plus,
                    '-' => Minus,
                    'x' => Times,
                    'i' => I,
                    _ => null
                };
            if (sprite == null) continue;
            
            if (outline)
                sprite.DrawOutlineCentered(new(x, center.Y), color);
            else
                sprite.DrawCentered(new(x, center.Y), color);
            
            x += 4;
        }
    }

    private void DrawDeferredRects() {
        if (!LocksmithHelperModule.LockSettings.UseShaders) {
            foreach (var tup in DeferredRectangles) {
                Draw.Rect(tup.Item1, tup.Item2, tup.Item3, tup.Item4, tup.Item5.ForceToColor());
            }
            return;
        }


        // Draw deferred rectangles (so we only stop the GameplayRenderer once)
        GameplayRenderer.End();
        
        List<int> normalRects = [];
        var matrix = (Scene as Level).Camera?.Matrix ?? Matrix.Identity;
        List<VertexPositionColorTexture> masterVerts = [];
        List<int> masterIndices = [];
        List<VertexPositionColorTexture> pureVerts = [];
        List<int> pureIndices = [];
        List<VertexPositionColorTexture> glitchVerts = [];
        List<int> glitchIndices = [];
        List<VertexPositionColorTexture> stoneVerts = [];
        List<int> stoneIndices = [];
        int index = 0;
        foreach (var tup in DeferredRectangles) {
            if (!(tup.Item5 is LockColor.Master or LockColor.Glitch or LockColor.Stone or LockColor.Pure)) {
                normalRects.Add(index++);
                continue;
            }
            var verts = tup.Item5 switch {
                LockColor.Master => masterVerts,
                LockColor.Glitch => glitchVerts,
                LockColor.Stone => stoneVerts,
                LockColor.Pure => pureVerts,
                _ => []
            };
            var indices = tup.Item5 switch {
                LockColor.Master => masterIndices,
                LockColor.Glitch => glitchIndices,
                LockColor.Stone => stoneIndices,
                LockColor.Pure => pureIndices,
                _ => []
            };
            var rectCount = verts.Count / 4;
            verts.Add(new VertexPositionColorTexture (new(tup.Item1, tup.Item2, 0), Color.White, new(0, 0)));
            verts.Add(new VertexPositionColorTexture (new(tup.Item1 + tup.Item3, tup.Item2, 0), Color.White, new(1, 0)));
            verts.Add(new VertexPositionColorTexture (new(tup.Item1, tup.Item2 + tup.Item4, 0), Color.White, new(0, 1)));
            verts.Add(new VertexPositionColorTexture (new(tup.Item1 + tup.Item3, tup.Item2 + tup.Item4, 0), Color.White, new(1, 1)));
            indices.AddRange([0 + rectCount, 1 + rectCount, 2 + rectCount, 1 + rectCount, 3 + rectCount, 2 + rectCount]);
            index++;
        }
        if (masterVerts.Count > 0)
            GFX.DrawIndexedVertices(
                matrix,
                masterVerts.ToArray(),
                masterVerts.Count,
                masterIndices.ToArray(),
                masterIndices.Count / 3,
                LocksmithHelperModule.MasterFx
            );
        if (glitchIndices.Count > 0)
            GFX.DrawIndexedVertices(
                matrix,
                glitchVerts.ToArray(),
                glitchVerts.Count,
                glitchIndices.ToArray(),
                glitchIndices.Count / 3,
                LocksmithHelperModule.GlitchFx
            );
        if (pureIndices.Count > 0)
            GFX.DrawIndexedVertices(
                matrix,
                pureVerts.ToArray(),
                pureVerts.Count,
                pureIndices.ToArray(),
                pureIndices.Count / 3,
                LocksmithHelperModule.PureFx
            );
        if (stoneVerts.Count > 0)
            GFX.DrawIndexedVertices(
                matrix,
                stoneVerts.ToArray(),
                stoneVerts.Count,
                stoneIndices.ToArray(),
                stoneIndices.Count / 3,
                LocksmithHelperModule.StoneFx
            );
        GameplayRenderer.Begin();
        foreach (int idx in normalRects) {
            var tup = DeferredRectangles[idx];
            Draw.Rect(tup.Item1, tup.Item2, tup.Item3, tup.Item4, tup.Item5.ForceToColor());
        }
    }

    private void RenderColor(float x, float y, float width, float height, LockColor color) =>
        DeferredRectangles.Add(new(x, y, width, height, color));

    private bool RenderSigil(Vector2 center, Complex value)
    {
        MTexture sprite;
        if (value.RealWithView() == 0) return false;

        if (value.Real != value.Real && value.Imaginary != value.Imaginary)
            // NaN, so it's All
            sprite = All;
        else if (!double.IsFinite(value.RealWithView()))
            sprite = Blast;
        else
            sprite = Keyhole;
        var sigilColor = value.RealWithView() < 0 ? Color.White : Color.Black;
        sprite.DrawCentered(center, sigilColor);
        if (sprite != Keyhole || Math.Abs(value.Real) == 1) return true;
        DrawComplex(value, new(center.X, center.Y + 7), sigilColor);
        return true;
    }

    #endregion
}