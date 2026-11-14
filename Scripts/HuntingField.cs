using Godot;
using System;
using System.Collections.Generic;

public partial class HuntingField : Control
{
    [Signal] public delegate void SafeTargetHitEventHandler(string animalId, string journalEntry);
    [Signal] public delegate void YoungAnimalPassedEventHandler(string animalName);
    [Signal] public delegate void ShotMissedEventHandler();

    [Export] public Texture2D FieldTexture { get; set; } = null!;
    [Export] public Texture2D RabbitTexture { get; set; } = null!;
    [Export] public Texture2D SquirrelTexture { get; set; } = null!;

    private sealed class Target
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public string AnimalId = "rabbit";
        public bool IsYoung;
        public float Radius;
        public float Phase;
    }

    private readonly List<Target> _targets = new();
    private readonly RandomNumberGenerator _rng = new();
    private Vector2 _aim;
    private Vector2 _lastShot;
    private float _shotPulse;
    private float _elapsed;
    private bool _active;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.All;
        MouseDefaultCursorShape = CursorShape.Cross;
        _rng.Randomize();
        _aim = Size / 2f;
        Resized += KeepAimInBounds;
        QueueRedraw();
    }

    public void StartRound()
    {
        _targets.Clear();
        _active = true;
        AddTarget("rabbit", false, 0.15f, 0.48f, 105f);
        AddTarget("squirrel", false, 0.78f, 0.74f, -122f);
        AddTarget("rabbit", true, 0.62f, 0.40f, -82f);
        AddTarget("squirrel", true, 0.35f, 0.66f, 90f);
        GrabFocus();
        QueueRedraw();
    }

    public void StopRound()
    {
        _active = false;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_shotPulse > 0)
        {
            _shotPulse = Mathf.Max(0, _shotPulse - (float)delta * 2.8f);
            QueueRedraw();
        }

        if (!_active)
        {
            return;
        }

        Vector2 controller = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (controller.LengthSquared() > 0.01f && HasFocus())
        {
            _aim += controller.Normalized() * 280f * (float)delta;
            KeepAimInBounds();
        }
        if (HasFocus() && Input.IsActionJustPressed("ui_accept"))
        {
            FireAt(_aim);
        }

        foreach (Target target in _targets)
        {
            target.Position += target.Velocity * (float)delta;
            if (target.Position.X < target.Radius)
            {
                target.Position.X = target.Radius;
                target.Velocity.X = Mathf.Abs(target.Velocity.X);
            }
            else if (target.Position.X > Size.X - target.Radius)
            {
                target.Position.X = Size.X - target.Radius;
                target.Velocity.X = -Mathf.Abs(target.Velocity.X);
            }
        }
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            _aim = motion.Position;
            KeepAimInBounds();
            QueueRedraw();
        }
        else if (@event is InputEventMouseButton button
            && button.ButtonIndex == MouseButton.Left
            && button.Pressed)
        {
            GrabFocus();
            _aim = button.Position;
            FireAt(_aim);
            AcceptEvent();
        }
        else if (@event is InputEventScreenDrag drag)
        {
            _aim = drag.Position;
            KeepAimInBounds();
            QueueRedraw();
            AcceptEvent();
        }
        else if (@event is InputEventScreenTouch touch && touch.Pressed)
        {
            GrabFocus();
            _aim = touch.Position;
            FireAt(_aim);
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        DrawBackdrop();

        var orderedTargets = new List<Target>(_targets);
        orderedTargets.Sort((left, right) => left.Position.Y.CompareTo(right.Position.Y));
        foreach (Target target in orderedTargets)
        {
            DrawAnimal(target);
        }

        if (_shotPulse > 0)
        {
            float pulseRadius = 10 + (1 - _shotPulse) * 34;
            DrawArc(_lastShot, pulseRadius, 0, Mathf.Tau, 36,
                new Color(1f, 0.88f, 0.42f, _shotPulse * 0.8f), 3);
        }

        DrawCrosshair();
        DrawRect(new Rect2(1, 1, Mathf.Max(0, Size.X - 2), Mathf.Max(0, Size.Y - 2)),
            new Color(0.24f, 0.13f, 0.045f, 0.95f), false, 4);
    }

    private void DrawBackdrop()
    {
        if (FieldTexture == null)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.34f, 0.51f, 0.18f));
            return;
        }

        Vector2 textureSize = FieldTexture.GetSize();
        float destinationAspect = Size.X / Mathf.Max(1, Size.Y);
        float textureAspect = textureSize.X / textureSize.Y;
        Rect2 source = new(Vector2.Zero, textureSize);
        if (textureAspect < destinationAspect)
        {
            float croppedHeight = textureSize.X / destinationAspect;
            source = new Rect2(0, (textureSize.Y - croppedHeight) * 0.42f,
                textureSize.X, croppedHeight);
        }
        else
        {
            float croppedWidth = textureSize.Y * destinationAspect;
            source = new Rect2((textureSize.X - croppedWidth) / 2f, 0,
                croppedWidth, textureSize.Y);
        }

        DrawTextureRectRegion(FieldTexture, new Rect2(Vector2.Zero, Size), source);
        DrawRect(new Rect2(0, 0, Size.X, 38), new Color(0.04f, 0.08f, 0.04f, 0.22f));
        DrawRect(new Rect2(0, Size.Y - 52, Size.X, 52), new Color(0.18f, 0.10f, 0.025f, 0.13f));
    }

    private void DrawAnimal(Target target)
    {
        Vector2 displayPosition = GetDisplayPosition(target);
        Vector2 drawSize = GetDrawSize(target);
        Texture2D texture = target.AnimalId == "rabbit" ? RabbitTexture : SquirrelTexture;

        DrawSetTransform(displayPosition, 0, new Vector2(1, 0.24f));
        DrawCircle(Vector2.Zero, drawSize.X * 0.31f, new Color(0.04f, 0.025f, 0.01f, 0.34f));
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        if (texture != null)
        {
            float direction = target.Velocity.X >= 0 ? 1 : -1;
            DrawSetTransform(displayPosition, 0, new Vector2(direction, 1));
            DrawTextureRect(texture,
                new Rect2(-drawSize.X / 2f, -drawSize.Y, drawSize.X, drawSize.Y),
                false,
                target.IsYoung ? new Color(0.92f, 0.95f, 1f) : Colors.White);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
        else
        {
            DrawCircle(displayPosition - new Vector2(0, drawSize.Y * 0.4f),
                drawSize.Y * 0.28f,
                target.AnimalId == "rabbit" ? new Color(0.7f, 0.6f, 0.48f) : new Color(0.65f, 0.32f, 0.13f));
        }

        if (target.IsYoung)
        {
            const float badgeWidth = 132;
            Vector2 badgePosition = displayPosition + new Vector2(-badgeWidth / 2f, -drawSize.Y - 27);
            DrawRect(new Rect2(badgePosition, new Vector2(badgeWidth, 22)), new Color(0.12f, 0.08f, 0.03f, 0.88f));
            DrawString(ThemeDB.FallbackFont, badgePosition + new Vector2(0, 16),
                "YOUNG — LET PASS", HorizontalAlignment.Center, badgeWidth, 13,
                new Color(1f, 0.92f, 0.52f));
        }
    }

    private void DrawCrosshair()
    {
        Color outline = new(0.08f, 0.045f, 0.015f, 0.92f);
        Color brass = new(1f, 0.82f, 0.34f, 0.98f);
        DrawArc(_aim, 20, 0, Mathf.Tau, 40, outline, 6);
        DrawLine(_aim - new Vector2(29, 0), _aim + new Vector2(29, 0), outline, 6);
        DrawLine(_aim - new Vector2(0, 29), _aim + new Vector2(0, 29), outline, 6);
        DrawArc(_aim, 20, 0, Mathf.Tau, 40, brass, 2.5f);
        DrawLine(_aim - new Vector2(29, 0), _aim - new Vector2(8, 0), brass, 2.5f);
        DrawLine(_aim + new Vector2(8, 0), _aim + new Vector2(29, 0), brass, 2.5f);
        DrawLine(_aim - new Vector2(0, 29), _aim - new Vector2(0, 8), brass, 2.5f);
        DrawLine(_aim + new Vector2(0, 8), _aim + new Vector2(0, 29), brass, 2.5f);
        DrawCircle(_aim, 2.5f, brass);
    }

    private void FireAt(Vector2 point)
    {
        if (!_active)
        {
            return;
        }

        _lastShot = point;
        _shotPulse = 1f;
        for (int index = _targets.Count - 1; index >= 0; index--)
        {
            Target target = _targets[index];
            Vector2 displayPosition = GetDisplayPosition(target);
            Vector2 drawSize = GetDrawSize(target);
            var hitBox = new Rect2(
                displayPosition.X - drawSize.X / 2f,
                displayPosition.Y - drawSize.Y,
                drawSize.X,
                drawSize.Y).Grow(8);
            if (!hitBox.HasPoint(point))
            {
                continue;
            }

            if (target.IsYoung)
            {
                EmitSignal(SignalName.YoungAnimalPassed,
                    target.AnimalId == "rabbit" ? "young rabbit" : "young squirrel");
                target.Velocity.X *= 1.45f;
                return;
            }

            string journal = target.AnimalId == "rabbit"
                ? "Rabbit — paired hind prints often land ahead of the smaller front prints."
                : "Squirrel — small four-toed front prints and five-toed hind prints often appear near trees.";
            EmitSignal(SignalName.SafeTargetHit, target.AnimalId, journal);
            _targets.RemoveAt(index);
            AddTarget(target.AnimalId == "rabbit" ? "squirrel" : "rabbit", false,
                target.Velocity.X > 0 ? 0.06f : 0.94f,
                _rng.RandfRange(0.43f, 0.76f),
                target.Velocity.X > 0 ? 116f : -116f);
            return;
        }

        EmitSignal(SignalName.ShotMissed);
    }

    private Vector2 GetDisplayPosition(Target target)
    {
        float depthScale = GetDepthScale(target);
        float bob = Mathf.Sin(_elapsed * 7f + target.Phase) * 3.5f * depthScale;
        return target.Position + new Vector2(0, bob);
    }

    private Vector2 GetDrawSize(Target target)
    {
        Vector2 baseSize = target.AnimalId == "rabbit" ? new Vector2(150, 94) : new Vector2(170, 84);
        float ageScale = target.IsYoung ? 0.68f : 1f;
        return baseSize * GetDepthScale(target) * ageScale;
    }

    private float GetDepthScale(Target target)
    {
        float depth = Mathf.Clamp((target.Position.Y / Mathf.Max(1, Size.Y) - 0.28f) / 0.52f, 0, 1);
        return Mathf.Lerp(0.64f, 1.04f, depth);
    }

    private void AddTarget(string animalId, bool young, float xRatio, float yRatio, float speed)
    {
        _targets.Add(new Target
        {
            AnimalId = animalId,
            IsYoung = young,
            Radius = animalId == "rabbit" ? 62f : 70f,
            Position = new Vector2(
                Mathf.Clamp(Size.X * xRatio, 72, Mathf.Max(72, Size.X - 72)),
                Mathf.Clamp(Size.Y * yRatio, 105, Mathf.Max(105, Size.Y - 28))),
            Velocity = new Vector2(speed, 0),
            Phase = _rng.RandfRange(0, Mathf.Tau)
        });
    }

    private void KeepAimInBounds()
    {
        _aim.X = Mathf.Clamp(_aim.X, 18, Mathf.Max(18, Size.X - 18));
        _aim.Y = Mathf.Clamp(_aim.Y, 18, Mathf.Max(18, Size.Y - 18));
    }
}
