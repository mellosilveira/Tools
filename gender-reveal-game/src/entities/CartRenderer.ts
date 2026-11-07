/**
 * 🏎️ CartRenderer: Co-op Adventure Kart & Passengers Engine
 *
 * Renders the parents (Mom & Dad) and the family Pet riding together
 * in a high-energy 2000s/2010s arcade roadster cart ("Carrinho"):
 * - Dynamic suspension bounce, tilt angles for jumps and ramps
 * - Spinning wheels with chrome rims and tire treads
 * - Nitro flames, exhaust smoke puffs, and headlight illumination cones
 * - Mom driving (holding the steering wheel), Dad cheering/waving, Pet enjoying the ride
 * - 3 distinct themes:
 *   1. 👕 Casual & Festa: Vibrant race roadster, racing stripes, party balloons on antenna
 *   2. ⚡ Cyberpunk 2077: Matte cyber racer, glowing neon cyan/magenta underglow, twin plasma thrusters
 *   3. 🐉 RPG Dragões: Golden dragon chariot with wing pauldrons, dragon head prow, flame trail
 */

import { ParentMomConfig, ParentDadConfig, PetConfig, OutfitTheme } from '../config/gameConfig';
import { CharacterRenderer } from './CharacterRenderer';

export interface CartAnimationState {
  x: number;
  y: number;
  vx: number;
  vy: number;
  tiltAngle: number; // in radians (-0.35 to +0.35)
  bounceY: number; // suspension vertical offset
  squashStretchX: number;
  squashStretchY: number;
  wheelRotation: number; // in radians
  isBoosting: boolean; // nitro rocket booster active
  isAirborne: boolean; // jumping in air
  eyeExpression: 'normal' | 'happy' | 'shocked' | 'stars';
}

export class CartRenderer {
  private static readonly INK_COLOR = '#160f29';

  public static renderCart(
    ctx: CanvasRenderingContext2D,
    momConfig: ParentMomConfig,
    dadConfig: ParentDadConfig,
    petConfig: PetConfig,
    state: CartAnimationState,
    scale = 1.0
  ): void {
    ctx.save();
    ctx.translate(state.x, state.y + state.bounceY);
    ctx.rotate(state.tiltAngle);
    ctx.scale(state.squashStretchX * scale, state.squashStretchY * scale);

    const theme: OutfitTheme = momConfig.outfitTheme || dadConfig.outfitTheme || 'casual';

    // -----------------------------------------------------------------
    // 1. GROUND SHADOW (Drawn only when near ground)
    // -----------------------------------------------------------------
    if (!state.isAirborne) {
      ctx.save();
      ctx.fillStyle = 'rgba(0, 0, 0, 0.35)';
      ctx.beginPath();
      ctx.ellipse(0, 32, 95, 14, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    }

    // -----------------------------------------------------------------
    // 2. EXHAUST FLAMES & SMOKE (Behind Cart)
    // -----------------------------------------------------------------
    this.renderExhaustEffects(ctx, theme, state.isBoosting, state.wheelRotation);

    // -----------------------------------------------------------------
    // 3. UNDERGLOW NEON (Cyberpunk & Arcade Glow)
    // -----------------------------------------------------------------
    if (theme === 'cyberpunk') {
      ctx.save();
      ctx.fillStyle = 'rgba(0, 210, 255, 0.35)';
      ctx.beginPath();
      ctx.ellipse(0, 26, 85, 10, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    }

    // -----------------------------------------------------------------
    // 4. REAR SEAT & CHASSIS BACKREST
    // -----------------------------------------------------------------
    this.renderChassisInterior(ctx, theme);

    // -----------------------------------------------------------------
    // 5. PASSENGERS (Pet, Dad, Mom)
    // -----------------------------------------------------------------
    // A. Pet in rear basket
    if (petConfig.enabled && petConfig.type !== 'none') {
      this.renderPetInCart(ctx, petConfig, theme, state.wheelRotation);
    }

    // B. Dad (Passenger / Cheering)
    this.renderDadInCart(ctx, dadConfig, state.eyeExpression, state.wheelRotation);

    // C. Mom (Driver / Steering)
    this.renderMomInCart(ctx, momConfig, state.eyeExpression, state.wheelRotation);

    // -----------------------------------------------------------------
    // 6. CART BODYWORK (Outer Chassis, Windshield, Hood, Grill)
    // -----------------------------------------------------------------
    this.renderChassisExterior(ctx, theme, momConfig.outfitColor, dadConfig.outfitColor);

    // -----------------------------------------------------------------
    // 7. STEERING WHEEL & DASHBOARD
    // -----------------------------------------------------------------
    this.renderDashboardAndSteering(ctx, state.wheelRotation);

    // -----------------------------------------------------------------
    // 8. WHEELS (Front & Rear, Spinning with Tires & Chrome Rims)
    // -----------------------------------------------------------------
    const rearWheelX = -58;
    const frontWheelX = 58;
    const wheelY = 24;
    const wheelRadius = 22;

    this.renderWheel(ctx, rearWheelX, wheelY, wheelRadius, state.wheelRotation, theme);
    this.renderWheel(ctx, frontWheelX, wheelY, wheelRadius, state.wheelRotation, theme);

    // -----------------------------------------------------------------
    // 9. HEADLIGHTS & LIGHT CONES
    // -----------------------------------------------------------------
    this.renderHeadlights(ctx, theme);

    ctx.restore();
  }

  // =========================================================================
  // 👥 PASSENGERS RENDERING (Mom, Dad, Pet in Cart)
  // =========================================================================

  /**
   * Renders Mom sitting in the driver's seat holding the steering wheel.
   */
  private static renderMomInCart(
    ctx: CanvasRenderingContext2D,
    config: ParentMomConfig,
    eyeExpression: 'normal' | 'happy' | 'shocked' | 'stars',
    wheelRotation: number
  ): void {
    ctx.save();
    // Driver seat position
    const seatX = 22;
    const seatY = -8;
    ctx.translate(seatX, seatY);

    const headY = -48;

    // 1. Back Hair (Blowing in the wind)
    this.renderMomBackHairWind(ctx, config.hairColor, config.hairStyle, headY, wheelRotation);

    // 2. Torso (Sitting behind steering wheel)
    ctx.fillStyle = config.outfitColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.0;
    ctx.beginPath();
    ctx.roundRect(-14, -32, 28, 30, [10, 10, 4, 4]);
    ctx.fill();
    ctx.stroke();

    // Torso highlight
    ctx.fillStyle = 'rgba(255, 255, 255, 0.2)';
    ctx.beginPath();
    ctx.roundRect(-11, -29, 8, 22, [5, 3, 3, 5]);
    ctx.fill();

    // Themed Collar
    if (config.outfitTheme === 'cyberpunk') {
      ctx.fillStyle = '#facc15';
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 2.0;
      ctx.beginPath();
      ctx.moveTo(-14, -34);
      ctx.lineTo(-7, -22);
      ctx.lineTo(7, -22);
      ctx.lineTo(14, -34);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    } else if (config.outfitTheme === 'dragon_knight') {
      // Golden pauldron
      ctx.fillStyle = '#d97706';
      ctx.strokeStyle = '#fef08a';
      ctx.lineWidth = 1.8;
      ctx.beginPath();
      ctx.moveTo(-14, -32);
      ctx.lineTo(-20, -26);
      ctx.lineTo(-10, -22);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    // 3. Driver Arms (Gripping steering wheel)
    ctx.strokeStyle = config.skinTone;
    ctx.lineWidth = 5.5;
    ctx.lineCap = 'round';

    // Left arm to wheel
    ctx.beginPath();
    ctx.moveTo(-10, -22);
    ctx.lineTo(8, -12);
    ctx.stroke();

    // Right arm to wheel
    ctx.beginPath();
    ctx.moveTo(10, -22);
    ctx.lineTo(16, -10);
    ctx.stroke();

    // Hands on wheel
    ctx.fillStyle = config.skinTone;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.arc(8, -12, 3.8, 0, Math.PI * 2);
    ctx.arc(16, -10, 3.8, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // 4. Neck & Sculpted Head
    CharacterRenderer.renderNeck(ctx, config.skinTone, headY);
    CharacterRenderer.renderHeadBase(ctx, config.skinTone, headY, true);

    // 5. 2000s/2010s Expressive Face
    CharacterRenderer.renderFace2000s(
      ctx,
      eyeExpression,
      headY,
      true,
      config.hairColor,
      config.outfitTheme,
      false
    );

    // 6. Front Hair & Angel Ring
    CharacterRenderer.renderMomFrontHair(ctx, config.hairColor, config.hairStyle, headY);

    ctx.restore();
  }

  /**
   * Renders Dad sitting in the passenger seat, cheering or waving.
   */
  private static renderDadInCart(
    ctx: CanvasRenderingContext2D,
    config: ParentDadConfig,
    eyeExpression: 'normal' | 'happy' | 'shocked' | 'stars',
    wheelRotation: number
  ): void {
    ctx.save();
    // Passenger seat (slightly to the left and raised for clear co-op visibility)
    const seatX = -18;
    const seatY = -12;
    ctx.translate(seatX, seatY);

    const headY = -50;
    const waveArmY = Math.sin(wheelRotation * 1.8) * 4;

    // 1. Torso
    ctx.fillStyle = config.outfitColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.0;
    ctx.beginPath();
    ctx.roundRect(-16, -34, 32, 32, [10, 10, 4, 4]);
    ctx.fill();
    ctx.stroke();

    // Highlight
    ctx.fillStyle = 'rgba(255, 255, 255, 0.18)';
    ctx.beginPath();
    ctx.roundRect(-13, -31, 9, 24, [5, 3, 3, 5]);
    ctx.fill();

    // Themed details
    if (config.outfitTheme === 'cyberpunk') {
      ctx.fillStyle = '#facc15';
      ctx.strokeStyle = '#ff2a85';
      ctx.lineWidth = 2.0;
      ctx.beginPath();
      ctx.moveTo(-16, -36);
      ctx.lineTo(-8, -24);
      ctx.lineTo(8, -24);
      ctx.lineTo(16, -36);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    } else if (config.outfitTheme === 'dragon_knight') {
      // Golden pauldron
      ctx.fillStyle = '#475569';
      ctx.strokeStyle = '#fbbf24';
      ctx.lineWidth = 2.0;
      ctx.beginPath();
      ctx.moveTo(-16, -34);
      ctx.lineTo(-24, -28);
      ctx.lineTo(-12, -24);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    // 2. Arms
    // Left arm resting on cart door
    ctx.strokeStyle = config.outfitTheme === 'cyberpunk' ? '#64748b' : config.skinTone;
    ctx.lineWidth = 6.0;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(-12, -24);
    ctx.lineTo(-24, -12);
    ctx.stroke();

    // Right arm raised cheering in victory ("YEAH! ✌️")
    ctx.beginPath();
    ctx.moveTo(12, -24);
    ctx.lineTo(24, -40 + waveArmY);
    ctx.stroke();

    // Victory hand / fist
    ctx.fillStyle = config.skinTone;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.arc(25, -42 + waveArmY, 4.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // 3. Neck & Heroic Head
    CharacterRenderer.renderNeck(ctx, config.skinTone, headY);
    CharacterRenderer.renderHeadBase(ctx, config.skinTone, headY, false);

    // 4. 2000s Expressive Face & Beard
    CharacterRenderer.renderFace2000s(
      ctx,
      eyeExpression,
      headY,
      false,
      config.hairColor,
      config.outfitTheme,
      config.hasBeard
    );

    // 5. Front Hair
    CharacterRenderer.renderDadHair(ctx, config.hairColor, config.hairStyle, headY);

    // 6. Eyewear (Glasses or Cyber Visor)
    CharacterRenderer.renderDadEyewear(ctx, config.outfitTheme, config.hasGlasses, headY);

    ctx.restore();
  }

  /**
   * Renders the Pet in the rear basket with flapping ears and goggles/bandana/wings!
   */
  private static renderPetInCart(
    ctx: CanvasRenderingContext2D,
    config: PetConfig,
    theme: OutfitTheme,
    wheelRotation: number
  ): void {
    ctx.save();
    // Rumble seat basket position (rear of the cart)
    const petX = -54;
    const petY = -14;
    ctx.translate(petX, petY);

    const earFlap = Math.sin(wheelRotation * 2.2) * 5;

    // Pet Body / Paws resting on the rim
    ctx.fillStyle = config.primaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.ellipse(0, 0, 13, 10, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Dragon Wings flapping behind pet
    if (theme === 'dragon_knight') {
      const wingFlap = Math.sin(wheelRotation * 3) * 0.35;
      ctx.save();
      ctx.translate(-4, -14);
      ctx.rotate(wingFlap);
      ctx.fillStyle = '#dc2626';
      ctx.strokeStyle = '#fbbf24';
      ctx.lineWidth = 1.8;
      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.lineTo(-14, -14);
      ctx.lineTo(-5, -10);
      ctx.lineTo(2, -18);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
      ctx.restore();
    }

    // Pet Head
    ctx.fillStyle = config.primaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.arc(4, -12, 10, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Cute Ears (Flapping in the wind!)
    ctx.fillStyle = config.secondaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.0;

    if (config.type === 'dog') {
      // Floppy dog ear flapping back
      ctx.beginPath();
      ctx.ellipse(-3, -12 + earFlap, 4, 8, -0.6, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    } else {
      // Pointy cat ear
      ctx.beginPath();
      ctx.moveTo(-1, -19);
      ctx.lineTo(3, -26);
      ctx.lineTo(7, -19);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    // Snout & Nose
    ctx.fillStyle = '#160f29';
    ctx.beginPath();
    ctx.arc(12, -10, 2.5, 0, Math.PI * 2);
    ctx.fill();

    // Sparkly Mascot Eye or Cyber Visor
    if (theme === 'cyberpunk') {
      ctx.fillStyle = '#ff2a85';
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 1.2;
      ctx.beginPath();
      ctx.roundRect(4, -15, 9, 5, 2);
      ctx.fill();
      ctx.stroke();
    } else {
      // Sparkling Mascot Eye
      ctx.fillStyle = '#160f29';
      ctx.beginPath();
      ctx.arc(6, -13, 2.8, 0, Math.PI * 2);
      ctx.fill();

      ctx.fillStyle = '#ffffff';
      ctx.beginPath();
      ctx.arc(7, -14, 1.2, 0, Math.PI * 2);
      ctx.fill();

      // Cute cheek blush
      ctx.fillStyle = 'rgba(255, 99, 132, 0.45)';
      ctx.beginPath();
      ctx.ellipse(6, -8, 2.5, 1.8, 0, 0, Math.PI * 2);
      ctx.fill();
    }

    // Front Paws resting on the cart edge
    ctx.fillStyle = config.secondaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.0;
    ctx.beginPath();
    ctx.ellipse(8, 2, 4, 3, 0, 0, Math.PI * 2);
    ctx.ellipse(14, 2, 4, 3, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    ctx.restore();
  }

  // =========================================================================
  // 🏎️ CART CHASSIS, INTERIOR & EXTERIOR BODYWORK
  // =========================================================================

  private static renderChassisInterior(ctx: CanvasRenderingContext2D, theme: OutfitTheme): void {
    ctx.save();
    // Inner tub shadow & leather seats
    ctx.fillStyle = '#1e1b2e'; // Dark interior
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.0;

    // Cockpit opening
    ctx.beginPath();
    ctx.roundRect(-68, -18, 110, 28, 8);
    ctx.fill();
    ctx.stroke();

    // Driver & Passenger Seat Headrests
    ctx.fillStyle = theme === 'cyberpunk' ? '#0f172a' : '#881337'; // Neon black or sports crimson
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;

    // Dad Headrest
    ctx.beginPath();
    ctx.roundRect(-26, -38, 16, 20, 6);
    ctx.fill();
    ctx.stroke();

    // Mom Headrest
    ctx.beginPath();
    ctx.roundRect(14, -36, 16, 20, 6);
    ctx.fill();
    ctx.stroke();

    ctx.restore();
  }

  private static renderChassisExterior(
    ctx: CanvasRenderingContext2D,
    theme: OutfitTheme,
    momColor: string,
    dadColor: string
  ): void {
    ctx.save();

    // Primary Body Colors
    let bodyColor = '#ef4444'; // Radiant Roadster Red
    let trimColor = '#fbbf24'; // Arcade Gold

    if (theme === 'cyberpunk') {
      bodyColor = '#0f172a'; // Carbon Black
      trimColor = '#00d2ff'; // Cyber Cyan
    } else if (theme === 'dragon_knight') {
      bodyColor = '#78350f'; // Dragon Bronze
      trimColor = '#f59e0b'; // Gold Scale
    } else {
      // Casual: Mix mom and dad outfit accents
      bodyColor = momColor || '#ef4444';
      trimColor = dadColor || '#fbbf24';
    }

    // --- 1. Main Aerodynamic Body Contour ---
    ctx.fillStyle = bodyColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.5;
    ctx.lineJoin = 'round';

    ctx.beginPath();
    // Rear bumper
    ctx.moveTo(-82, 18);
    ctx.lineTo(-84, -4);
    // Rear spoiler rise
    ctx.quadraticCurveTo(-82, -18, -68, -20);
    ctx.lineTo(-46, -14);
    // Cockpit door drop
    ctx.lineTo(-40, -4);
    ctx.lineTo(26, -4);
    // Windshield base to hood
    ctx.lineTo(36, -8);
    ctx.quadraticCurveTo(58, -6, 78, 6);
    // Front nose & grill
    ctx.lineTo(84, 18);
    // Bottom chassis line with wheel arches
    ctx.lineTo(76, 20);
    // Front wheel cutout
    ctx.arc(58, 22, 25, 0, Math.PI, true);
    ctx.lineTo(-34, 20);
    // Rear wheel cutout
    ctx.arc(-58, 22, 25, 0, Math.PI, true);
    ctx.lineTo(-82, 18);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // --- 2. Metallic / Gloss Body Highlight Stripe ---
    ctx.fillStyle = 'rgba(255, 255, 255, 0.28)';
    ctx.beginPath();
    ctx.moveTo(-62, -10);
    ctx.lineTo(-42, -2);
    ctx.lineTo(32, -2);
    ctx.lineTo(72, 8);
    ctx.lineTo(70, 12);
    ctx.lineTo(30, 2);
    ctx.lineTo(-44, 2);
    ctx.lineTo(-64, -6);
    ctx.closePath();
    ctx.fill();

    // --- 3. Thematic Graphics & Decals ---
    if (theme === 'cyberpunk') {
      // Neon Circuit Trim Lines
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 2.2;
      ctx.beginPath();
      ctx.moveTo(-74, 10);
      ctx.lineTo(-30, 10);
      ctx.lineTo(-10, 16);
      ctx.lineTo(40, 16);
      ctx.lineTo(70, 12);
      ctx.stroke();

      // Cyber Decal "CYBER 2077"
      ctx.fillStyle = '#ff2a85';
      ctx.font = 'bold 9px monospace';
      ctx.fillText('CYBER-P3', -12, 12);
    } else if (theme === 'dragon_knight') {
      // Golden Dragon Wing Fins on Rear & Fender
      ctx.fillStyle = '#fbbf24';
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = 2.0;

      // Rear Dragon Wing Fin
      ctx.beginPath();
      ctx.moveTo(-78, -14);
      ctx.lineTo(-94, -28);
      ctx.lineTo(-80, -22);
      ctx.lineTo(-72, -32);
      ctx.lineTo(-66, -16);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();

      // Golden Dragon Crest on Door
      ctx.fillStyle = '#dc2626';
      ctx.beginPath();
      ctx.arc(0, 8, 6, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#fbbf24';
      ctx.stroke();
    } else {
      // Casual: Classic Racing Stripe (Trim Color & White Border)
      ctx.fillStyle = trimColor;
      ctx.fillRect(-70, 6, 140, 5);
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(-70, 8, 140, 1.5);

      // Party Balloons tied to rear antenna!
      this.renderPartyBalloons(ctx, -76, -18);
    }

    // --- 4. Tinted Curved Windshield with Glint ---
    ctx.save();
    ctx.fillStyle = 'rgba(0, 210, 255, 0.45)';
    ctx.strokeStyle = '#00d2ff';
    ctx.lineWidth = 2.5;

    ctx.beginPath();
    ctx.moveTo(28, -6);
    ctx.quadraticCurveTo(34, -28, 42, -32);
    ctx.lineTo(47, -32);
    ctx.quadraticCurveTo(43, -16, 40, -6);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Windshield diagonal glint reflection
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.75)';
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.moveTo(34, -22);
    ctx.lineTo(41, -12);
    ctx.stroke();
    ctx.restore();

    // --- 5. Front Bumper & Chrome Grill ---
    ctx.fillStyle = '#e2e8f0'; // Chrome silver
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;

    ctx.beginPath();
    ctx.roundRect(74, 10, 12, 10, 3);
    ctx.fill();
    ctx.stroke();

    ctx.restore();
  }

  private static renderDashboardAndSteering(ctx: CanvasRenderingContext2D, wheelRotation: number): void {
    ctx.save();
    const dashX = 35;
    const dashY = -4;

    // Steering Column
    ctx.strokeStyle = '#334155';
    ctx.lineWidth = 4.0;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(dashX + 5, dashY + 4);
    ctx.lineTo(dashX - 4, dashY - 6);
    ctx.stroke();

    // Steering Wheel Rim
    const steerAngle = Math.sin(wheelRotation * 1.5) * 0.25;
    ctx.save();
    ctx.translate(dashX - 4, dashY - 6);
    ctx.rotate(steerAngle);

    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.ellipse(0, 0, 8, 12, -0.3, 0, Math.PI * 2);
    ctx.stroke();

    // Chrome center horn button
    ctx.fillStyle = '#fbbf24';
    ctx.beginPath();
    ctx.arc(0, 0, 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();

    ctx.restore();
  }

  // =========================================================================
  // 🛞 WHEELS & RIMS (Spinning with Speed)
  // =========================================================================

  private static renderWheel(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    radius: number,
    rotation: number,
    theme: OutfitTheme
  ): void {
    ctx.save();
    ctx.translate(x, y);

    // 1. Black Tire Rubber with treads
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.0;
    ctx.beginPath();
    ctx.arc(0, 0, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Tire tread rim
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.12)';
    ctx.lineWidth = 2.0;
    ctx.beginPath();
    ctx.arc(0, 0, radius - 3, 0, Math.PI * 2);
    ctx.stroke();

    // 2. Spinning Inner Rim & Spokes
    ctx.rotate(rotation);

    // Rim base
    const rimColor = theme === 'cyberpunk' ? '#00d2ff' : theme === 'dragon_knight' ? '#f59e0b' : '#e2e8f0';
    ctx.fillStyle = '#1e293b';
    ctx.beginPath();
    ctx.arc(0, 0, radius - 6, 0, Math.PI * 2);
    ctx.fill();

    // 5-Spoke Star Design
    ctx.strokeStyle = rimColor;
    ctx.lineWidth = 3.0;
    ctx.lineCap = 'round';
    for (let i = 0; i < 5; i++) {
      const angle = (i * Math.PI * 2) / 5;
      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.lineTo(Math.cos(angle) * (radius - 7), Math.sin(angle) * (radius - 7));
      ctx.stroke();
    }

    // Chrome Center Hubcap
    ctx.fillStyle = rimColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.arc(0, 0, 4.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    ctx.restore();
  }

  // =========================================================================
  // 💡 HEADLIGHTS, EXHAUST & SPECIAL EFFECTS
  // =========================================================================

  private static renderHeadlights(ctx: CanvasRenderingContext2D, theme: OutfitTheme): void {
    ctx.save();
    const lightX = 84;
    const lightY = 12;

    // Glowing Headlight Bulb
    const beamColor = theme === 'cyberpunk' ? '#00d2ff' : '#fef08a';
    ctx.fillStyle = beamColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.0;
    ctx.beginPath();
    ctx.arc(lightX, lightY, 5.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Forward Light Cone Beam
    const grad = ctx.createLinearGradient(lightX, lightY, lightX + 160, lightY);
    grad.addColorStop(0, theme === 'cyberpunk' ? 'rgba(0, 210, 255, 0.45)' : 'rgba(254, 240, 138, 0.45)');
    grad.addColorStop(1, 'rgba(254, 240, 138, 0.0)');

    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.moveTo(lightX + 4, lightY - 4);
    ctx.lineTo(lightX + 180, lightY - 26);
    ctx.lineTo(lightX + 180, lightY + 36);
    ctx.lineTo(lightX + 4, lightY + 6);
    ctx.closePath();
    ctx.fill();

    ctx.restore();
  }

  private static renderExhaustEffects(
    ctx: CanvasRenderingContext2D,
    theme: OutfitTheme,
    isBoosting: boolean,
    wheelRotation: number
  ): void {
    ctx.save();
    const exhaustX = -84;
    const exhaustY = 12;

    // Twin Exhaust Metal Pipes
    ctx.fillStyle = '#475569';
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.0;
    ctx.fillRect(exhaustX, exhaustY - 3, 6, 6);
    ctx.strokeRect(exhaustX, exhaustY - 3, 6, 6);

    if (isBoosting) {
      // Giant Nitro Rocket Plume
      const flameLength = 45 + Math.sin(wheelRotation * 6) * 12;
      const flameColor = theme === 'cyberpunk' ? '#00d2ff' : '#f97316';
      const innerFlame = theme === 'cyberpunk' ? '#ffffff' : '#facc15';

      // Outer Flame Plume
      ctx.fillStyle = flameColor;
      ctx.beginPath();
      ctx.moveTo(exhaustX - 2, exhaustY - 7);
      ctx.lineTo(exhaustX - flameLength, exhaustY);
      ctx.lineTo(exhaustX - 2, exhaustY + 7);
      ctx.closePath();
      ctx.fill();

      // Inner Hot Core
      ctx.fillStyle = innerFlame;
      ctx.beginPath();
      ctx.moveTo(exhaustX - 2, exhaustY - 3.5);
      ctx.lineTo(exhaustX - flameLength * 0.65, exhaustY);
      ctx.lineTo(exhaustX - 2, exhaustY + 3.5);
      ctx.closePath();
      ctx.fill();
    } else {
      // Subtle cartoon exhaust puff
      const puffScale = 0.8 + Math.sin(wheelRotation * 2) * 0.2;
      ctx.fillStyle = 'rgba(203, 213, 225, 0.45)';
      ctx.beginPath();
      ctx.arc(exhaustX - 10, exhaustY - 2, 5 * puffScale, 0, Math.PI * 2);
      ctx.arc(exhaustX - 18, exhaustY - 6, 7 * puffScale, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.restore();
  }

  private static renderPartyBalloons(ctx: CanvasRenderingContext2D, x: number, y: number): void {
    ctx.save();
    ctx.translate(x, y);

    // Antenna string
    ctx.strokeStyle = '#94a3b8';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(0, 0);
    ctx.quadraticCurveTo(-6, -20, -10, -38);
    ctx.stroke();

    // 3 Floating Balloons (Cyan, Gold, Pink)
    const balloons = [
      { bx: -16, by: -46, color: '#00d2ff' },
      { bx: -6, by: -52, color: '#fbbf24' },
      { bx: 2, by: -44, color: '#ff2a85' }
    ];

    balloons.forEach((b) => {
      // String
      ctx.strokeStyle = '#cbd5e1';
      ctx.lineWidth = 1.0;
      ctx.beginPath();
      ctx.moveTo(-10, -38);
      ctx.lineTo(b.bx, b.by + 8);
      ctx.stroke();

      // Balloon body
      ctx.fillStyle = b.color;
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = 1.8;
      ctx.beginPath();
      ctx.ellipse(b.bx, b.by, 7, 9, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Highlight
      ctx.fillStyle = '#ffffff';
      ctx.beginPath();
      ctx.arc(b.bx - 2, b.by - 3, 2, 0, Math.PI * 2);
      ctx.fill();
    });

    ctx.restore();
  }

  private static renderMomBackHairWind(
    ctx: CanvasRenderingContext2D,
    hairColor: string,
    style: string,
    headY: number,
    wheelRotation: number
  ): void {
    ctx.save();
    ctx.fillStyle = hairColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.0;
    ctx.lineJoin = 'round';

    const windWave = Math.sin(wheelRotation * 1.5) * 6;

    if (style === 'bun') {
      ctx.beginPath();
      ctx.arc(-2, headY - 24, 12, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    } else {
      // Wavy locks streaming backward in the wind!
      ctx.beginPath();
      ctx.moveTo(-8, headY - 8);
      ctx.bezierCurveTo(-26, headY - 4, -40, headY + 4 + windWave, -52, headY + 14);
      ctx.quadraticCurveTo(-42, headY + 18, -32, headY + 12);
      ctx.bezierCurveTo(-22, headY + 6, -14, headY + 2, -4, headY + 6);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    ctx.restore();
  }
}
