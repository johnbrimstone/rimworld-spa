using RimWorld;
using UnityEngine;
using Verse;

namespace SpaMod
{
    public class CompProperties_SaunaHeaterGlowOverlay : CompProperties
    {
        public CompProperties_SaunaHeaterGlowOverlay()
        {
            compClass = typeof(CompSaunaHeaterGlowOverlay);
        }
    }

    // Draws Glow1.png — a small glowing-ember sprite matching the heating-element region
    // on SaunaHeaterElectric.png (SaunaHeaterElectric_m.png is the mask used to align it
    // during art creation; not consumed at runtime here, since Glow1.png is already
    // pre-cropped to the right shape/position on the same 128x128 canvas as the base
    // sprite, so drawing it at the same transform lines it up automatically) on top of
    // the heater whenever it's powered, with a gentle pulsing scale for a "living embers"
    // feel. Same PostDraw-overlay technique vanilla uses for CompFireOverlay — power-gated
    // instead of fuel-gated, and a single static frame instead of Graphic_Flicker's
    // multi-frame fire animation, since we only have one glow image. Requires the heater
    // def's drawerType to be MapMeshAndRealTime — PostDraw never runs for MapMeshOnly.
    // [StaticConstructorOnStartup] (as on CompFireOverlay) so GlowGraphic's texture loads
    // on the main thread during startup, not lazily on first draw.
    [StaticConstructorOnStartup]
    public class CompSaunaHeaterGlowOverlay : ThingComp
    {
        private const float PulseSpeed = 2f;
        private const float PulseMagnitude = 0.08f;

        // Matches CompFireOverlay's own altitude nudge — draws one step above the parent
        // so the overlay doesn't z-fight with the base sprite.
        private const float AltitudeOffset = 0.03658537f;

        private static readonly Graphic GlowGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Glow1", ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);

        public override void PostDraw()
        {
            base.PostDraw();

            CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return;
            }

            float pulse = 1f + PulseMagnitude * Mathf.Sin(Time.realtimeSinceStartup * PulseSpeed + parent.thingIDNumber);
            Vector3 drawPos = parent.DrawPos;
            drawPos.y += AltitudeOffset;

            Matrix4x4 matrix = default;
            matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(pulse, 1f, pulse));
            Graphics.DrawMesh(MeshPool.plane10, matrix, GlowGraphic.MatSingle, 0);
        }
    }
}
