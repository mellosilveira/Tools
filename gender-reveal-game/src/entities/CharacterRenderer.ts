/**
 * 🎨 CharacterRenderer: 2000s & 2010s Golden-Age 2D Arcade & Chibi Character Engine
 *
 * Inspired by the clean, iconic, expressive 2D art style of the 2000s and 2010s:
 * - Scott Pilgrim vs. The World: The Game (2010)
 * - MapleStory & Ragnarok Online (2002-2004 classic sprite charm)
 * - Castle Crashers & The Behemoth (2008)
 * - Rayman Origins & Legends (2011/2013 clean cartoon vectors)
 * - Shantae & WayForward classics
 *
 * Key Visual Hallmarks:
 * 1. Harmonious, beautifully contoured heads with cute rounded chins and cheeks (never deformed!).
 * 2. Big, sparkling, expressive 2D anime/arcade eyes with dual specular catchlights and elegant lashlines.
 * 3. Warm, joyful open arcade smiles with clean tooth highlights and rosy mouth depth.
 * 4. Classic 2000s soft pill blushes with cute highlight slashes.
 * 5. Volumetric hair with layered bangs, flowing back locks, and the iconic 2000s "Angel Ring" sheen.
 * 6. Perfectly sculpted beards, stylish glasses, and high-tech Cyberpunk / Dragon RPG costumes.
 */

import { ParentMomConfig, ParentDadConfig, PetConfig, Gender } from '../config/gameConfig';

export interface CharacterAnimationState {
  x: number;
  y: number;
  vx: number;
  vy: number;
  isGrounded: boolean;
  isJumping: boolean;
  runCycle: number;
  squashStretchX: number;
  squashStretchY: number;
  eyeExpression: 'normal' | 'happy' | 'shocked' | 'stars';
  facingRight: boolean;
}

export class CharacterRenderer {
  // Constant Palette for Ink & Shadows
  private static readonly INK_COLOR = '#160f29'; // Deep arcade ink
  private static readonly SHADOW_COLOR = 'rgba(22, 15, 41, 0.18)'; // Soft ambient shadow

  /**
   * Renders Mom (Player 1) with 2000s/2010s arcade heroine charm, gorgeous hair, and themed costumes.
   */
  public static renderMom(
    ctx: CanvasRenderingContext2D,
    config: ParentMomConfig,
    state: CharacterAnimationState,
    scale = 1.0
  ): void {
    ctx.save();
    ctx.translate(state.x, state.y);
    ctx.scale(state.squashStretchX * scale, state.squashStretchY * scale);

    const legSwing = Math.sin(state.runCycle) * 20;
    const bounceY = Math.abs(Math.sin(state.runCycle)) * 5;

    // Ground Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.32)';
    ctx.beginPath();
    ctx.ellipse(0, 5, 26, 7, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.translate(0, -bounceY);

    const headY = -76;

    // ------------------------------------------
    // 1. BACK HAIR & RPG CAPE (Behind Character)
    // ------------------------------------------
    if (config.outfitTheme === 'dragon_knight') {
      this.renderDragonCape(ctx, state.runCycle, '#991b1b', '#f59e0b', -10, -48);
    }

    this.renderMomBackHair(ctx, config.hairColor, config.hairStyle, headY);

    // ------------------------------------------
    // 2. LEGS & FEET
    // ------------------------------------------
    const pantsColor = config.outfitTheme === 'cyberpunk' ? '#0f172a' : config.outfitTheme === 'dragon_knight' ? '#1e293b' : '#334155';
    ctx.lineWidth = 7.5;
    ctx.lineCap = 'round';
    ctx.strokeStyle = pantsColor;

    // Back leg
    ctx.beginPath();
    ctx.moveTo(4, -18);
    ctx.lineTo(7 - legSwing * 0.65, 0);
    ctx.stroke();

    // Front leg
    ctx.beginPath();
    ctx.moveTo(-4, -18);
    ctx.lineTo(-5 + legSwing * 0.65, 0);
    ctx.stroke();

    // Shoes / Boots
    const bootColor = config.outfitTheme === 'cyberpunk' ? '#00d2ff' : config.outfitTheme === 'dragon_knight' ? '#f59e0b' : '#ffffff';
    ctx.fillStyle = bootColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;

    // Back boot
    ctx.beginPath();
    ctx.roundRect(3 - legSwing * 0.65, -4, 13, 7.5, 3.5);
    ctx.fill();
    ctx.stroke();

    // Front boot
    ctx.beginPath();
    ctx.roundRect(-9 + legSwing * 0.65, -4, 13, 7.5, 3.5);
    ctx.fill();
    ctx.stroke();

    // ------------------------------------------
    // 3. TORSO & THEMED COSTUME
    // ------------------------------------------
    ctx.fillStyle = config.outfitColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.2;

    // Torso Base Silhouette (Cute fitted arcade proportion)
    ctx.beginPath();
    ctx.roundRect(-15, -54, 30, 36, [12, 12, 6, 6]);
    ctx.fill();
    ctx.stroke();

    // Ambient Torso Highlight
    ctx.fillStyle = 'rgba(255, 255, 255, 0.22)';
    ctx.beginPath();
    ctx.roundRect(-12, -51, 9, 30, [6, 4, 4, 6]);
    ctx.fill();

    // Themed Details
    if (config.outfitTheme === 'cyberpunk') {
      this.renderCyberpunkTorsoMom(ctx);
    } else if (config.outfitTheme === 'dragon_knight') {
      this.renderDragonTorsoMom(ctx);
    } else {
      this.renderCasualTorsoMom(ctx);
    }

    // ------------------------------------------
    // 4. ARMS & HANDS
    // ------------------------------------------
    ctx.lineWidth = 5.5;
    ctx.lineCap = 'round';
    ctx.strokeStyle = config.skinTone;

    // Back arm
    ctx.beginPath();
    ctx.moveTo(-11, -46);
    ctx.lineTo(-16 - legSwing * 0.45, -33);
    ctx.stroke();

    // Front arm
    ctx.beginPath();
    ctx.moveTo(11, -46);
    ctx.lineTo(16 + legSwing * 0.45, -33);
    ctx.stroke();

    // ------------------------------------------
    // 5. NECK & SCULPTED HEAD
    // ------------------------------------------
    this.renderNeck(ctx, config.skinTone, headY);
    this.renderHeadBase(ctx, config.skinTone, headY, true);

    // ------------------------------------------
    // 6. 2000s/2010s EXPRESSIVE FACE
    // ------------------------------------------
    this.renderFace2000s(
      ctx,
      state.eyeExpression,
      headY,
      true,
      config.hairColor,
      config.outfitTheme,
      false
    );

    // ------------------------------------------
    // 7. FRONT HAIR (Bangs, Curls & Angel Ring)
    // ------------------------------------------
    this.renderMomFrontHair(ctx, config.hairColor, config.hairStyle, headY);

    ctx.restore();
  }

  /**
   * Renders Dad (Player 2) with 2000s/2010s arcade hero style, sharp beard, and themed costumes.
   */
  public static renderDad(
    ctx: CanvasRenderingContext2D,
    config: ParentDadConfig,
    state: CharacterAnimationState,
    scale = 1.0
  ): void {
    ctx.save();
    ctx.translate(state.x, state.y);
    ctx.scale(state.squashStretchX * scale, state.squashStretchY * scale);

    const legSwing = Math.sin(state.runCycle + 0.3) * 20;
    const bounceY = Math.abs(Math.sin(state.runCycle + 0.3)) * 5;

    // Ground Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.32)';
    ctx.beginPath();
    ctx.ellipse(0, 5, 29, 8, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.translate(0, -bounceY);

    const headY = -80;

    // ------------------------------------------
    // 1. DRAGON RPG VELVET CAPE (Behind Character)
    // ------------------------------------------
    if (config.outfitTheme === 'dragon_knight') {
      this.renderDragonCape(ctx, state.runCycle + 0.4, '#1e1b4b', '#d97706', -12, -50);
    }

    // ------------------------------------------
    // 2. LEGS & BOOTS
    // ------------------------------------------
    const pantsColor = config.outfitTheme === 'cyberpunk' ? '#090d16' : config.outfitTheme === 'dragon_knight' ? '#1e293b' : '#334155';
    ctx.lineWidth = 8.5;
    ctx.lineCap = 'round';
    ctx.strokeStyle = pantsColor;

    // Back leg
    ctx.beginPath();
    ctx.moveTo(5, -20);
    ctx.lineTo(9 - legSwing * 0.65, 0);
    ctx.stroke();

    // Front leg
    ctx.beginPath();
    ctx.moveTo(-5, -20);
    ctx.lineTo(-6 + legSwing * 0.65, 0);
    ctx.stroke();

    // Shoes
    const shoeColor = config.outfitTheme === 'cyberpunk' ? '#facc15' : config.outfitTheme === 'dragon_knight' ? '#64748b' : '#ffffff';
    ctx.fillStyle = shoeColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;

    // Back shoe
    ctx.beginPath();
    ctx.roundRect(5 - legSwing * 0.65, -5, 15, 8.5, 4);
    ctx.fill();
    ctx.stroke();

    // Front shoe
    ctx.beginPath();
    ctx.roundRect(-10 + legSwing * 0.65, -5, 15, 8.5, 4);
    ctx.fill();
    ctx.stroke();

    // ------------------------------------------
    // 3. TORSO & THEMED CHEST
    // ------------------------------------------
    ctx.fillStyle = config.outfitColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.2;

    // Heroic Arcade Torso Silhouette
    ctx.beginPath();
    ctx.roundRect(-18, -58, 36, 40, [12, 12, 6, 6]);
    ctx.fill();
    ctx.stroke();

    // Ambient Torso Highlight
    ctx.fillStyle = 'rgba(255, 255, 255, 0.2)';
    ctx.beginPath();
    ctx.roundRect(-14, -55, 10, 34, [6, 4, 4, 6]);
    ctx.fill();

    // Themed Torso Details
    if (config.outfitTheme === 'cyberpunk') {
      this.renderCyberpunkTorsoDad(ctx);
    } else if (config.outfitTheme === 'dragon_knight') {
      this.renderDragonTorsoDad(ctx);
    } else {
      this.renderCasualTorsoDad(ctx);
    }

    // ------------------------------------------
    // 4. ARMS (CHROME CYBER-ARM OPTION)
    // ------------------------------------------
    ctx.lineWidth = 6.5;
    ctx.lineCap = 'round';

    // Back arm
    ctx.strokeStyle = config.skinTone;
    ctx.beginPath();
    ctx.moveTo(-13, -50);
    ctx.lineTo(-19 - legSwing * 0.45, -34);
    ctx.stroke();

    // Front arm (Chrome Bionic Arm in Cyberpunk!)
    if (config.outfitTheme === 'cyberpunk') {
      ctx.strokeStyle = '#64748b'; // Chrome
      ctx.beginPath();
      ctx.moveTo(13, -50);
      ctx.lineTo(19 + legSwing * 0.45, -34);
      ctx.stroke();

      // Glowing Neon Cyber-Line
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(14, -46);
      ctx.lineTo(18 + legSwing * 0.45, -34);
      ctx.stroke();
    } else {
      ctx.strokeStyle = config.skinTone;
      ctx.beginPath();
      ctx.moveTo(13, -50);
      ctx.lineTo(19 + legSwing * 0.45, -34);
      ctx.stroke();
    }

    // ------------------------------------------
    // 5. NECK & HEROIC HEAD
    // ------------------------------------------
    this.renderNeck(ctx, config.skinTone, headY);
    this.renderHeadBase(ctx, config.skinTone, headY, false);

    // ------------------------------------------
    // 6. 2000s/2010s EXPRESSIVE FACE & BEARD
    // ------------------------------------------
    this.renderFace2000s(
      ctx,
      state.eyeExpression,
      headY,
      false,
      config.hairColor,
      config.outfitTheme,
      config.hasBeard
    );

    // ------------------------------------------
    // 7. FRONT HAIR (Fade, Spiky, Curly & Angel Ring)
    // ------------------------------------------
    this.renderDadHair(ctx, config.hairColor, config.hairStyle, headY);

    // ------------------------------------------
    // 8. GLASSES & CYBER VISOR OVERLAYS
    // ------------------------------------------
    this.renderDadEyewear(ctx, config.outfitTheme, config.hasGlasses, headY);

    ctx.restore();
  }

  /**
   * Renders Pet with 2000s mascot charm (MapleStory / Ragnarok / Castle Crashers style).
   */
  public static renderPet(
    ctx: CanvasRenderingContext2D,
    config: PetConfig,
    state: CharacterAnimationState,
    scale = 1.0
  ): void {
    if (!config.enabled || config.type === 'none') return;

    ctx.save();
    ctx.translate(state.x, state.y);
    ctx.scale(state.squashStretchX * scale, state.squashStretchY * scale);

    const legCycle = Math.sin(state.runCycle * 1.5) * 12;
    const tailWag = Math.sin(state.runCycle * 2) * 20;

    // Ground Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.28)';
    ctx.beginPath();
    ctx.ellipse(0, 5, 20, 6, 0, 0, Math.PI * 2);
    ctx.fill();

    // ------------------------------------------
    // DRAGON RPG: FLAPPING BAT/DRAGON WINGS
    // ------------------------------------------
    if (config.outfitTheme === 'dragon_knight') {
      const wingFlap = Math.sin(state.runCycle * 3.5) * 0.35;
      ctx.save();
      ctx.translate(0, -20);
      ctx.rotate(wingFlap);
      ctx.fillStyle = '#dc2626';
      ctx.strokeStyle = '#fbbf24';
      ctx.lineWidth = 2.2;

      ctx.beginPath();
      ctx.moveTo(-3, 0);
      ctx.lineTo(-18, -20);
      ctx.lineTo(-7, -15);
      ctx.lineTo(2, -24);
      ctx.lineTo(6, 0);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
      ctx.restore();
    }

    // Tail
    ctx.strokeStyle = config.primaryColor;
    ctx.lineWidth = 5.5;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(-13, -15);
    ctx.quadraticCurveTo(-22, -26 + tailWag, -24, -18);
    ctx.stroke();

    // Cyberpunk Neon Tail Tip
    if (config.outfitTheme === 'cyberpunk') {
      ctx.fillStyle = '#00d2ff';
      ctx.beginPath();
      ctx.arc(-24, -18, 4, 0, Math.PI * 2);
      ctx.fill();
    }

    // Legs
    ctx.strokeStyle = config.primaryColor;
    ctx.lineWidth = 4.5;
    ctx.beginPath();
    ctx.moveTo(-9, -8);
    ctx.lineTo(-9 - legCycle, 0);
    ctx.moveTo(9, -8);
    ctx.lineTo(9 + legCycle, 0);
    ctx.stroke();

    // Body (Cute round mascot shape)
    ctx.fillStyle = config.primaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.8;
    ctx.beginPath();
    ctx.ellipse(0, -14, 17, 12, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Belly patch
    ctx.fillStyle = config.secondaryColor;
    ctx.beginPath();
    ctx.ellipse(0, -12, 10, 7.5, 0, 0, Math.PI * 2);
    ctx.fill();

    // Head
    const headX = 11;
    const headY = -25;
    ctx.fillStyle = config.primaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.8;
    ctx.beginPath();
    ctx.arc(headX, headY, 13, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Dragon Horns
    if (config.outfitTheme === 'dragon_knight') {
      ctx.fillStyle = '#fbbf24';
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(9, -35);
      ctx.lineTo(7, -44);
      ctx.lineTo(15, -36);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    // Ears
    ctx.fillStyle = config.secondaryColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.2;
    if (config.type === 'dog') {
      // Floppy cute dog ears
      ctx.beginPath();
      ctx.ellipse(7, -27, 4.5, 9, 0.35, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    } else {
      // Pointy cat ears
      ctx.beginPath();
      ctx.moveTo(5, -35);
      ctx.lineTo(9, -44);
      ctx.lineTo(14, -35);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    // Snout / Nose
    ctx.fillStyle = '#160f29';
    ctx.beginPath();
    ctx.arc(headX + 9, headY + 2, 2.8, 0, Math.PI * 2);
    ctx.fill();

    // Mascot Eye / Visor
    if (config.outfitTheme === 'cyberpunk') {
      // Cyber HUD Visor
      ctx.fillStyle = '#ff2a85';
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.roundRect(headX - 2, headY - 4, 11, 6, 2);
      ctx.fill();
      ctx.stroke();
    } else {
      // 2000s Sparkly Mascot Eye
      ctx.fillStyle = '#160f29';
      ctx.beginPath();
      ctx.arc(headX + 3, headY - 2, 3.5, 0, Math.PI * 2);
      ctx.fill();

      // Specular highlight
      ctx.fillStyle = '#ffffff';
      ctx.beginPath();
      ctx.arc(headX + 4.2, headY - 3.2, 1.4, 0, Math.PI * 2);
      ctx.fill();

      // Rosy cheek blush
      ctx.fillStyle = 'rgba(255, 99, 132, 0.45)';
      ctx.beginPath();
      ctx.ellipse(headX + 3, headY + 4, 3, 2, 0, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.restore();
  }

  /**
   * Renders Baby Player 3 at the Grand Reveal (Heroic 2000s Chibi Unlocked Character).
   */
  public static renderBabyPlayer3(
    ctx: CanvasRenderingContext2D,
    gender: Gender,
    x: number,
    y: number,
    scale: number,
    themeColors: { boyPrimary: string; girlPrimary: string; arcadeGold: string }
  ): void {
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(scale, scale);

    const glowColor = gender === 'boy' ? themeColors.boyPrimary : themeColors.girlPrimary;

    // Outer Aura Radiance
    ctx.fillStyle = gender === 'boy' ? 'rgba(0, 210, 255, 0.15)' : 'rgba(255, 42, 133, 0.15)';
    ctx.beginPath();
    ctx.arc(0, -10, 42, 0, Math.PI * 2);
    ctx.fill();

    // Swaddle Blanket / Heroic Cape
    ctx.fillStyle = glowColor;
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.ellipse(0, 2, 23, 28, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Golden Ribbon Belt
    ctx.fillStyle = themeColors.arcadeGold;
    ctx.strokeStyle = '#b45309';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(-13, -3, 26, 9, 4.5);
    ctx.fill();
    ctx.stroke();

    // Baby Head (Charming 2000s Round Chibi)
    ctx.fillStyle = '#fde2cc';
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.arc(0, -25, 17, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Mini Golden Arcade Crown
    ctx.fillStyle = themeColors.arcadeGold;
    ctx.strokeStyle = '#b45309';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(-11, -36);
    ctx.lineTo(-7, -48);
    ctx.lineTo(0, -39);
    ctx.lineTo(7, -48);
    ctx.lineTo(11, -36);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Ruby/Sapphire crown gem
    ctx.fillStyle = gender === 'boy' ? '#00d2ff' : '#ff2a85';
    ctx.beginPath();
    ctx.arc(0, -40, 2.5, 0, Math.PI * 2);
    ctx.fill();

    // Happy Blushing Cheeks
    ctx.fillStyle = 'rgba(255, 75, 115, 0.45)';
    ctx.beginPath();
    ctx.ellipse(-7.5, -21, 3.8, 2.4, 0, 0, Math.PI * 2);
    ctx.ellipse(7.5, -21, 3.8, 2.4, 0, 0, Math.PI * 2);
    ctx.fill();

    // Cute Joyful Crescent Eyes (^ ^)
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.8;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.arc(-6.5, -25, 3.5, Math.PI * 0.9, Math.PI * 2.1, false);
    ctx.stroke();

    ctx.beginPath();
    ctx.arc(6.5, -25, 3.5, Math.PI * 0.9, Math.PI * 2.1, false);
    ctx.stroke();

    // Cute Pacifier / Smile
    ctx.fillStyle = '#ffffff';
    ctx.strokeStyle = glowColor;
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.arc(0, -16, 5.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Pacifier ring
    ctx.strokeStyle = themeColors.arcadeGold;
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.arc(0, -12, 3, 0, Math.PI);
    ctx.stroke();

    ctx.restore();
  }

  // =========================================================================
  // 🏛️ CORE 2000s/2010s ANATOMY ENGINE: HEAD, FACE, EYES & EXPRESSIONS
  // =========================================================================

  /**
   * Renders the small neck connecting body to head with shaded depth.
   */
  public static renderNeck(ctx: CanvasRenderingContext2D, skinTone: string, headY: number): void {
    ctx.save();
    // Warm shadow under chin
    ctx.fillStyle = 'rgba(0, 0, 0, 0.16)';
    ctx.fillRect(-5, headY + 14, 10, 8);
    // Neck
    ctx.fillStyle = skinTone;
    ctx.fillRect(-4.5, headY + 15, 9, 7);
    ctx.restore();
  }

  /**
   * Renders the sculpted, organic head base for 2000s/2010s 2D games:
   * Smooth rounded crown, gently curved cheekbones, and soft tapered chin (no deformed bowling balls!).
   */
  public static renderHeadBase(
    ctx: CanvasRenderingContext2D,
    skinTone: string,
    headY: number,
    isFemale: boolean
  ): void {
    ctx.save();
    ctx.translate(0, headY);

    const hw = isFemale ? 19.5 : 20.5; // Half width
    const chinY = isFemale ? 17.5 : 18.5; // Chin bottom
    const topY = isFemale ? -19.5 : -20.5; // Crown top

    // --- 1. Cute Ears (Drawn first so head overlaps cleanly) ---
    ctx.fillStyle = skinTone;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;

    // Left Ear
    ctx.beginPath();
    ctx.ellipse(-hw + 1, -1, 3.8, 5.5, -0.15, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Left inner ear contour
    ctx.strokeStyle = 'rgba(230, 80, 110, 0.35)';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(-hw + 1.5, -1, 2.2, 0.5 * Math.PI, 1.5 * Math.PI, false);
    ctx.stroke();

    // Right Ear
    ctx.fillStyle = skinTone;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.ellipse(hw - 1, -1, 3.8, 5.5, 0.15, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Right inner ear contour
    ctx.strokeStyle = 'rgba(230, 80, 110, 0.35)';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(hw - 1.5, -1, 2.2, 1.5 * Math.PI, 0.5 * Math.PI, false);
    ctx.stroke();

    // --- 2. Head Silhouette (Organic, gentle chin taper) ---
    ctx.fillStyle = skinTone;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.2;
    ctx.lineJoin = 'round';

    ctx.beginPath();
    // Top crown arch
    ctx.moveTo(-hw + 3, -10);
    ctx.bezierCurveTo(-hw, topY + 4, -8, topY, 0, topY);
    ctx.bezierCurveTo(8, topY, hw, topY + 4, hw - 3, -10);

    // Cheekbones curve slightly out, then taper to rounded chin
    ctx.bezierCurveTo(hw + 0.5, -1, hw - 1, 9, 8, chinY - 2.5);
    ctx.quadraticCurveTo(0, chinY + 1.5, -8, chinY - 2.5);
    ctx.bezierCurveTo(-hw + 1, 9, -hw - 0.5, -1, -hw + 3, -10);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // --- 3. Soft ambient jawline shadow for 2000s depth ---
    ctx.fillStyle = this.SHADOW_COLOR;
    ctx.beginPath();
    ctx.moveTo(-10, chinY - 3.5);
    ctx.quadraticCurveTo(0, chinY + 0.5, 10, chinY - 3.5);
    ctx.quadraticCurveTo(0, chinY - 1.5, -10, chinY - 3.5);
    ctx.fill();

    ctx.restore();
  }

  /**
   * Renders the 2000s/2010s Expressive Face:
   * Gorgeous sparkling anime-arcade eyes, warm open smile, soft pill blushes, and cute button nose.
   */
  public static renderFace2000s(
    ctx: CanvasRenderingContext2D,
    expression: 'normal' | 'happy' | 'shocked' | 'stars',
    headY: number,
    isFemale: boolean,
    hairColor: string,
    outfitTheme: string,
    hasBeard: boolean
  ): void {
    ctx.save();
    ctx.translate(0, headY);

    const eyeSpacing = isFemale ? 8.2 : 8.8;
    const eyeY = isFemale ? -1.5 : -2.5;

    // ------------------------------------------
    // 1. CHEEK BLUSHES (2000s Arcade Signature)
    // ------------------------------------------
    ctx.fillStyle = 'rgba(255, 75, 115, 0.42)';
    ctx.beginPath();
    ctx.ellipse(-eyeSpacing - 2, 4.5, 4.5, 2.6, 0, 0, Math.PI * 2);
    ctx.ellipse(eyeSpacing + 2, 4.5, 4.5, 2.6, 0, 0, Math.PI * 2);
    ctx.fill();

    // Cute diagonal blush slashes
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.55)';
    ctx.lineWidth = 1.1;
    ctx.beginPath();
    ctx.moveTo(-eyeSpacing - 3.5, 3.5);
    ctx.lineTo(-eyeSpacing - 1.5, 5.5);
    ctx.moveTo(-eyeSpacing - 1.5, 3.5);
    ctx.lineTo(-eyeSpacing + 0.5, 5.5);

    ctx.moveTo(eyeSpacing + 0.5, 3.5);
    ctx.lineTo(eyeSpacing + 2.5, 5.5);
    ctx.moveTo(eyeSpacing + 2.5, 3.5);
    ctx.lineTo(eyeSpacing + 4.5, 5.5);
    ctx.stroke();

    // Cyberpunk cheek tattoo / tech line
    if (outfitTheme === 'cyberpunk' && isFemale) {
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 1.4;
      ctx.beginPath();
      ctx.moveTo(eyeSpacing + 5, 2);
      ctx.lineTo(eyeSpacing + 10, 3.5);
      ctx.lineTo(eyeSpacing + 12, 1);
      ctx.stroke();
    }

    // ------------------------------------------
    // 2. THE EYES (Golden Era 2000s/2010s Style)
    // ------------------------------------------
    if (expression === 'shocked') {
      this.renderShockedEyes(ctx, eyeSpacing, eyeY);
    } else if (expression === 'happy') {
      this.renderHappyEyes(ctx, eyeSpacing, eyeY, isFemale);
    } else if (expression === 'stars') {
      this.renderStarEyes(ctx, eyeSpacing, eyeY);
    } else {
      this.renderNormalEyes(ctx, eyeSpacing, eyeY, isFemale);
    }

    // ------------------------------------------
    // 3. EYEBROWS (Expressive & Friendly)
    // ------------------------------------------
    const browY = eyeY - (isFemale ? 6.5 : 6.0);
    ctx.strokeStyle = '#1e1633';
    ctx.lineWidth = isFemale ? 1.8 : 2.5;
    ctx.lineCap = 'round';

    // Left brow
    ctx.beginPath();
    ctx.moveTo(-eyeSpacing - 4.5, browY + 0.8);
    ctx.quadraticCurveTo(-eyeSpacing, browY - 1.6, -eyeSpacing + 3.8, browY + 0.2);
    ctx.stroke();

    // Right brow
    ctx.beginPath();
    ctx.moveTo(eyeSpacing - 3.8, browY + 0.2);
    ctx.quadraticCurveTo(eyeSpacing, browY - 1.6, eyeSpacing + 4.5, browY + 0.8);
    ctx.stroke();

    // ------------------------------------------
    // 4. CUTE BUTTON NOSE
    // ------------------------------------------
    ctx.fillStyle = '#b87258';
    ctx.beginPath();
    ctx.arc(0, 3.2, 1.2, 0, Math.PI * 2);
    ctx.fill();

    // ------------------------------------------
    // 5. BEARD (DAD) - Wrapped cleanly along jaw
    // ------------------------------------------
    if (!isFemale && hasBeard) {
      this.renderDadBeard(ctx, hairColor, outfitTheme);
    }

    // ------------------------------------------
    // 6. WARM JOYFUL ARCADE SMILE
    // ------------------------------------------
    this.renderArcadeSmile(ctx, isFemale, hasBeard);

    ctx.restore();
  }

  /**
   * Signature 2000s 2D Arcade Eyes:
   * Sclera oval + rich iris gradient + dual catchlights + elegant upper lashline.
   */
  private static renderNormalEyes(
    ctx: CanvasRenderingContext2D,
    eyeSpacing: number,
    eyeY: number,
    isFemale: boolean
  ): void {
    const eyeW = isFemale ? 4.5 : 4.2;
    const eyeH = isFemale ? 5.8 : 5.2;

    [-eyeSpacing, eyeSpacing].forEach((cx, idx) => {
      ctx.save();
      ctx.translate(cx, eyeY);

      // 1. Sclera (White eye background)
      ctx.fillStyle = '#ffffff';
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = 1.8;
      ctx.beginPath();
      ctx.ellipse(0, 0, eyeW, eyeH, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // 2. Iris (Multi-tone rich depth)
      const irisGrad = ctx.createLinearGradient(0, -eyeH, 0, eyeH);
      irisGrad.addColorStop(0, '#140c24'); // Deep obsidian top
      irisGrad.addColorStop(0.55, '#311b4a'); // Midnight violet
      irisGrad.addColorStop(1, '#581c87'); // Radiant base
      ctx.fillStyle = irisGrad;

      ctx.beginPath();
      ctx.ellipse(0, 0.4, eyeW * 0.82, eyeH * 0.88, 0, 0, Math.PI * 2);
      ctx.fill();

      // 3. Dark Pupil
      ctx.fillStyle = '#090514';
      ctx.beginPath();
      ctx.ellipse(0, 0.2, eyeW * 0.45, eyeH * 0.5, 0, 0, Math.PI * 2);
      ctx.fill();

      // 4. Primary Specular Catchlight (Upper Left)
      ctx.fillStyle = '#ffffff';
      ctx.beginPath();
      ctx.arc(-1.4, -1.8, 1.6, 0, Math.PI * 2);
      ctx.fill();

      // 5. Secondary Specular Twinkle (Bottom Right)
      ctx.beginPath();
      ctx.arc(1.3, 1.8, 0.9, 0, Math.PI * 2);
      ctx.fill();

      // 6. Upper Eyelid & Winged Eyelash
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = isFemale ? 2.6 : 2.2;
      ctx.lineCap = 'round';

      ctx.beginPath();
      ctx.arc(0, -0.6, eyeW * 1.05, Math.PI * 1.15, Math.PI * 1.85, false);
      ctx.stroke();

      // Winged Eyelashes for Mom
      if (isFemale) {
        const isRightEye = idx === 1;
        ctx.lineWidth = 2.0;
        ctx.beginPath();
        if (isRightEye) {
          ctx.moveTo(eyeW * 0.85, -1.5);
          ctx.quadraticCurveTo(eyeW + 2, -3.2, eyeW + 4, -4.5);
        } else {
          ctx.moveTo(-eyeW * 0.85, -1.5);
          ctx.quadraticCurveTo(-eyeW - 2, -3.2, -eyeW - 4, -4.5);
        }
        ctx.stroke();
      }

      ctx.restore();
    });
  }

  /**
   * Joyful Arcade Crescent Eyes (^ ^) for Jump and Level Up!
   */
  private static renderHappyEyes(
    ctx: CanvasRenderingContext2D,
    eyeSpacing: number,
    eyeY: number,
    isFemale: boolean
  ): void {
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = isFemale ? 3.0 : 2.6;
    ctx.lineCap = 'round';

    [-eyeSpacing, eyeSpacing].forEach((cx, idx) => {
      ctx.save();
      ctx.translate(cx, eyeY);

      // Crescent curve
      ctx.beginPath();
      ctx.arc(0, 1.2, 4.8, Math.PI * 1.15, Math.PI * 1.85, false);
      ctx.stroke();

      // Eyelash flicks for Mom
      if (isFemale) {
        const isRight = idx === 1;
        ctx.lineWidth = 2.0;
        ctx.beginPath();
        if (isRight) {
          ctx.moveTo(4, -1);
          ctx.lineTo(6.5, -3);
        } else {
          ctx.moveTo(-4, -1);
          ctx.lineTo(-6.5, -3);
        }
        ctx.stroke();
      }

      ctx.restore();
    });
  }

  /**
   * Shocked / Troll Surprise Eyes!
   */
  private static renderShockedEyes(ctx: CanvasRenderingContext2D, eyeSpacing: number, eyeY: number): void {
    [-eyeSpacing, eyeSpacing].forEach((cx) => {
      ctx.save();
      ctx.translate(cx, eyeY);

      // Wide white circle
      ctx.fillStyle = '#ffffff';
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = 2.2;
      ctx.beginPath();
      ctx.arc(0, 0, 5.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Pinpoint pupil
      ctx.fillStyle = '#090514';
      ctx.beginPath();
      ctx.arc(0, 0, 1.8, 0, Math.PI * 2);
      ctx.fill();

      ctx.restore();
    });

    // Floating blue cartoon sweat drop on temple
    ctx.fillStyle = '#38bdf8';
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 1.4;
    ctx.beginPath();
    ctx.moveTo(eyeSpacing + 9, eyeY - 8);
    ctx.quadraticCurveTo(eyeSpacing + 12, eyeY - 5, eyeSpacing + 9.5, eyeY - 2);
    ctx.arc(eyeSpacing + 8, eyeY - 2, 2, 0, Math.PI);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
  }

  /**
   * Victory Golden Stars in Eyes!
   */
  private static renderStarEyes(ctx: CanvasRenderingContext2D, eyeSpacing: number, eyeY: number): void {
    [-eyeSpacing, eyeSpacing].forEach((cx) => {
      ctx.save();
      ctx.translate(cx, eyeY);

      // Star shape
      ctx.fillStyle = '#fbbf24';
      ctx.strokeStyle = '#d97706';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      for (let i = 0; i < 5; i++) {
        const a1 = ((18 + i * 72) * Math.PI) / 180;
        const a2 = ((54 + i * 72) * Math.PI) / 180;
        ctx.lineTo(Math.cos(a1) * 5.2, -Math.sin(a1) * 5.2);
        ctx.lineTo(Math.cos(a2) * 2.3, -Math.sin(a2) * 2.3);
      }
      ctx.closePath();
      ctx.fill();
      ctx.stroke();

      // White center twinkle
      ctx.fillStyle = '#ffffff';
      ctx.beginPath();
      ctx.arc(-1, -1, 1.2, 0, Math.PI * 2);
      ctx.fill();

      ctx.restore();
    });
  }

  /**
   * Warm, joyful open 2000s arcade smile:
   * Curved top lip + rich crimson open cavity + clean white tooth line + rosy tongue.
   */
  private static renderArcadeSmile(ctx: CanvasRenderingContext2D, isFemale: boolean, hasBeard: boolean): void {
    const mouthY = hasBeard ? 8.5 : 8.0;
    const mouthW = isFemale ? 5.2 : 5.8;
    const mouthH = isFemale ? 4.2 : 4.6;

    ctx.save();
    ctx.translate(0, mouthY);

    // Mouth Cavity
    ctx.fillStyle = '#be123c'; // Deep warm berry
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.0;

    ctx.beginPath();
    ctx.moveTo(-mouthW, 0);
    ctx.quadraticCurveTo(0, -0.8, mouthW, 0);
    ctx.quadraticCurveTo(0, mouthH * 1.8, -mouthW, 0);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Crisp White Upper Tooth Highlight
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.moveTo(-mouthW * 0.75, 0.2);
    ctx.quadraticCurveTo(0, -0.5, mouthW * 0.75, 0.2);
    ctx.lineTo(mouthW * 0.6, 1.6);
    ctx.quadraticCurveTo(0, 1.8, -mouthW * 0.6, 1.6);
    ctx.closePath();
    ctx.fill();

    // Rosy Tongue
    ctx.fillStyle = '#fb7185';
    ctx.beginPath();
    ctx.ellipse(0, mouthH * 0.95, mouthW * 0.55, mouthH * 0.5, 0, 0, Math.PI);
    ctx.fill();

    // Cute Smile Corner Dimples
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 1.4;
    ctx.beginPath();
    ctx.moveTo(-mouthW - 0.8, -0.6);
    ctx.lineTo(-mouthW + 0.4, 0.6);
    ctx.moveTo(mouthW + 0.8, -0.6);
    ctx.lineTo(mouthW - 0.4, 0.6);
    ctx.stroke();

    ctx.restore();
  }

  /**
   * Groomed, stylish beard for Dad that wraps naturally along the jawline.
   */
  private static renderDadBeard(ctx: CanvasRenderingContext2D, hairColor: string, outfitTheme: string): void {
    ctx.save();
    ctx.fillStyle = hairColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.2;
    ctx.lineJoin = 'round';

    // Jawline Beard Strap & Chin Goatee
    ctx.beginPath();
    ctx.moveTo(-16, 2);
    ctx.quadraticCurveTo(-14, 12, -7, 16.5);
    ctx.quadraticCurveTo(0, 19.5, 7, 16.5);
    ctx.quadraticCurveTo(14, 12, 16, 2);
    ctx.quadraticCurveTo(12, 10, 6, 13);
    ctx.quadraticCurveTo(0, 14.5, -6, 13);
    ctx.quadraticCurveTo(-12, 10, -16, 2);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Neat Trimmed Mustache (Resting cleanly under the nose)
    ctx.beginPath();
    ctx.moveTo(-6, 5.5);
    ctx.quadraticCurveTo(-3, 4.0, 0, 4.6);
    ctx.quadraticCurveTo(3, 4.0, 6, 5.5);
    ctx.quadraticCurveTo(3, 7.5, 0, 6.8);
    ctx.quadraticCurveTo(-3, 7.5, -6, 5.5);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Cyberpunk Neon Chin Accent
    if (outfitTheme === 'cyberpunk') {
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(-4, 15);
      ctx.lineTo(0, 17.5);
      ctx.lineTo(4, 15);
      ctx.stroke();
    }

    ctx.restore();
  }

  // =========================================================================
  // 💇 VOLUMETRIC HAIR WITH 2000s "ANGEL RING" SHEEN
  // =========================================================================

  /**
   * Mom's Back Hair (Rendered behind the head & body).
   */
  private static renderMomBackHair(
    ctx: CanvasRenderingContext2D,
    hairColor: string,
    style: string,
    headY: number
  ): void {
    ctx.save();
    ctx.fillStyle = hairColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.2;
    ctx.lineJoin = 'round';

    if (style === 'bun') {
      // Chic Top Bun with Ribbon
      ctx.beginPath();
      ctx.arc(0, headY - 26, 13, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Bun Hair Texture & Ring
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.28)';
      ctx.lineWidth = 2.0;
      ctx.beginPath();
      ctx.arc(0, headY - 26, 8, Math.PI * 0.8, Math.PI * 1.8, false);
      ctx.stroke();

      // Golden Hair Pin / Ribbon
      ctx.fillStyle = '#fbbf24';
      ctx.beginPath();
      ctx.roundRect(-8, headY - 17, 16, 5, 2.5);
      ctx.fill();
    } else if (style === 'curly') {
      // Voluminous cascading curls behind shoulders
      [-17, 17].forEach((sideX) => {
        ctx.beginPath();
        ctx.arc(sideX, headY + 5, 9, 0, Math.PI * 2);
        ctx.arc(sideX * 1.1, headY + 18, 9.5, 0, Math.PI * 2);
        ctx.arc(sideX * 0.9, headY + 30, 8.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
      });
    } else if (style === 'short') {
      // Neat nape taper
      ctx.beginPath();
      ctx.arc(0, headY + 2, 22, 0.1 * Math.PI, 0.9 * Math.PI, false);
      ctx.fill();
      ctx.stroke();
    } else {
      // Classic Wavy / Long Flowing Locks (The 2000s Anime/Arcade Flow!)
      [-16, 16].forEach((sideX) => {
        ctx.beginPath();
        ctx.moveTo(sideX * 0.8, headY - 8);
        ctx.bezierCurveTo(sideX * 1.4, headY + 10, sideX * 1.3, headY + 26, sideX * 1.1, headY + 38);
        ctx.quadraticCurveTo(sideX * 0.8, headY + 41, sideX * 0.4, headY + 36);
        ctx.bezierCurveTo(sideX * 0.7, headY + 24, sideX * 0.6, headY + 12, sideX * 0.3, headY - 2);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
      });
    }

    ctx.restore();
  }

  /**
   * Mom's Front Hair (Sculpted bangs, framing side locks, and 2000s Angel Ring).
   */
  public static renderMomFrontHair(
    ctx: CanvasRenderingContext2D,
    hairColor: string,
    style: string,
    headY: number
  ): void {
    ctx.save();
    ctx.translate(0, headY);

    ctx.fillStyle = hairColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.2;
    ctx.lineJoin = 'round';

    // 1. Crown Volume Arch & Bangs
    ctx.beginPath();
    ctx.moveTo(-21, -6);
    ctx.bezierCurveTo(-23, -24, 0, -25, 21, -6);
    ctx.bezierCurveTo(23, -2, 20, 8, 17, 10);
    ctx.lineTo(15, 2);

    if (style === 'bun') {
      // Elegant parted fringe with hair ribbon
      ctx.quadraticCurveTo(7, -6, 0, -2);
      ctx.quadraticCurveTo(-7, -6, -15, 2);
    } else if (style === 'curly') {
      // Playful textured curl bangs
      ctx.quadraticCurveTo(9, -7, 3, -2);
      ctx.quadraticCurveTo(-3, -11, -9, -3);
      ctx.quadraticCurveTo(-12, -8, -15, 2);
    } else {
      // Sculpted Front Bangs (Side-swept anime locks)
      ctx.quadraticCurveTo(8, -8, 2, -4);
      ctx.quadraticCurveTo(-6, -11, -15, 2);
    }

    ctx.lineTo(-17, 10);
    ctx.bezierCurveTo(-20, 8, -23, -2, -21, -6);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // 2. The Iconic 2000s "Angel Ring" (Curved Hair Sheen Band)
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.38)';
    ctx.lineWidth = 2.8;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.arc(0, -7, 16, Math.PI * 1.2, Math.PI * 1.8, false);
    ctx.stroke();

    // 3. Side Framing Locks (Mechas)
    ctx.fillStyle = hairColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 2.6;

    // Left Lock
    ctx.beginPath();
    ctx.moveTo(-18, -4);
    ctx.quadraticCurveTo(-20, 10, -17, 18);
    ctx.quadraticCurveTo(-14, 12, -15, -2);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Right Lock
    ctx.beginPath();
    ctx.moveTo(18, -4);
    ctx.quadraticCurveTo(20, 10, 17, 18);
    ctx.quadraticCurveTo(14, 12, 15, -2);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    ctx.restore();
  }

  /**
   * Dad's Hair (Sculpted Fade, Curly, Spiky Hero, or Bald + Angel Ring).
   */
  public static renderDadHair(
    ctx: CanvasRenderingContext2D,
    hairColor: string,
    style: string,
    headY: number
  ): void {
    if (style === 'bald') {
      // Shaved scalp with neat sideburns
      ctx.save();
      ctx.translate(0, headY);
      ctx.fillStyle = hairColor;
      ctx.fillRect(-20, -5, 2.5, 8);
      ctx.fillRect(17.5, -5, 2.5, 8);
      // Scalp shine
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.3)';
      ctx.lineWidth = 2.2;
      ctx.beginPath();
      ctx.arc(0, -9, 15, Math.PI * 1.25, Math.PI * 1.75, false);
      ctx.stroke();
      ctx.restore();
      return;
    }

    ctx.save();
    ctx.translate(0, headY);

    ctx.fillStyle = hairColor;
    ctx.strokeStyle = this.INK_COLOR;
    ctx.lineWidth = 3.2;
    ctx.lineJoin = 'round';

    if (style === 'curly') {
      // Volumetric arcade textured curls
      ctx.beginPath();
      ctx.moveTo(-21, -6);
      ctx.bezierCurveTo(-23, -25, -5, -27, 0, -25);
      ctx.bezierCurveTo(5, -27, 23, -25, 21, -6);
      ctx.lineTo(18, -2);
      ctx.quadraticCurveTo(10, -10, 0, -8);
      ctx.quadraticCurveTo(-10, -10, -18, -2);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();

      // Top curl puffs
      ctx.beginPath();
      ctx.arc(-11, -22, 6.5, 0, Math.PI * 2);
      ctx.arc(0, -25, 7.5, 0, Math.PI * 2);
      ctx.arc(11, -22, 6.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    } else if (style === 'fade') {
      // Modern textured arcade fade / pompadour
      ctx.beginPath();
      ctx.moveTo(-21, -6);
      ctx.lineTo(-20, -16);
      ctx.quadraticCurveTo(-14, -28, 2, -26);
      ctx.quadraticCurveTo(14, -24, 21, -12);
      ctx.lineTo(21, -6);
      ctx.lineTo(17, -2);
      ctx.quadraticCurveTo(8, -12, 0, -9);
      ctx.quadraticCurveTo(-8, -12, -17, -2);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();

      // Fade side gradient
      ctx.fillStyle = 'rgba(0, 0, 0, 0.25)';
      ctx.fillRect(-20, -5, 3.5, 7);
      ctx.fillRect(16.5, -5, 3.5, 7);
    } else {
      // Classic Spiky / Parted Hero Hair (Scott Pilgrim / Dan the Man style)
      ctx.beginPath();
      ctx.moveTo(-21, -6);
      ctx.lineTo(-18, -19);
      ctx.lineTo(-12, -26);
      ctx.lineTo(-4, -21);
      ctx.lineTo(2, -28);
      ctx.lineTo(10, -22);
      ctx.lineTo(18, -24);
      ctx.lineTo(21, -6);
      ctx.lineTo(17, -2);
      ctx.quadraticCurveTo(8, -11, 0, -8);
      ctx.quadraticCurveTo(-8, -11, -17, -2);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    // Angel Ring Sheen
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.35)';
    ctx.lineWidth = 2.5;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.arc(0, -8, 16, Math.PI * 1.22, Math.PI * 1.78, false);
    ctx.stroke();

    ctx.restore();
  }

  /**
   * Dad's Eyewear: Classic Chic Glasses or Cyberpunk Holographic Visor.
   */
  public static renderDadEyewear(
    ctx: CanvasRenderingContext2D,
    outfitTheme: string,
    hasGlasses: boolean,
    headY: number
  ): void {
    ctx.save();
    ctx.translate(0, headY);

    if (outfitTheme === 'cyberpunk') {
      // Wrap-around Cyber Visor
      ctx.fillStyle = 'rgba(0, 210, 255, 0.88)';
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 2.2;

      ctx.beginPath();
      ctx.roundRect(-16, -7, 32, 10, 3.5);
      ctx.fill();
      ctx.stroke();

      // Scanline crosshairs & HUD
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(-3, -3, 6, 2);
      ctx.fillStyle = '#ff2a85';
      ctx.fillRect(8, -5, 3, 2);
    } else if (hasGlasses) {
      // Classic Stylish Rectangular Glasses
      ctx.strokeStyle = this.INK_COLOR;
      ctx.lineWidth = 2.6;
      ctx.fillStyle = 'rgba(255, 255, 255, 0.15)';

      // Left Frame
      ctx.beginPath();
      ctx.roundRect(-14, -6.5, 10.5, 9, 2);
      ctx.fill();
      ctx.stroke();

      // Right Frame
      ctx.beginPath();
      ctx.roundRect(3.5, -6.5, 10.5, 9, 2);
      ctx.fill();
      ctx.stroke();

      // Bridge
      ctx.beginPath();
      ctx.moveTo(-3.5, -2);
      ctx.lineTo(3.5, -2);
      ctx.stroke();

      // Lens Reflection Gleam
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.7)';
      ctx.lineWidth = 1.2;
      ctx.beginPath();
      ctx.moveTo(-11, -4.5);
      ctx.lineTo(-7, -0.5);
      ctx.moveTo(6.5, -4.5);
      ctx.lineTo(10.5, -0.5);
      ctx.stroke();
    }

    ctx.restore();
  }

  // =========================================================================
  // 🛡️ COSTUMES & ACCESSORIES (CASUAL, CYBERPUNK 2077, RPG DRAGÕES)
  // =========================================================================

  private static renderDragonCape(
    ctx: CanvasRenderingContext2D,
    runCycle: number,
    capeColor: string,
    goldTrim: string,
    startX: number,
    startY: number
  ): void {
    ctx.save();
    const wave = Math.sin(runCycle * 1.5) * 8;
    ctx.fillStyle = capeColor;
    ctx.strokeStyle = goldTrim;
    ctx.lineWidth = 2.5;

    ctx.beginPath();
    ctx.moveTo(startX, startY);
    ctx.quadraticCurveTo(-38, -20 + wave, -30, 8);
    ctx.lineTo(-8, 5);
    ctx.lineTo(-6, startY + 18);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.restore();
  }

  private static renderCyberpunkTorsoMom(ctx: CanvasRenderingContext2D): void {
    // Popped Samurai Jacket Collar
    ctx.fillStyle = '#facc15';
    ctx.strokeStyle = '#00d2ff';
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.moveTo(-16, -60);
    ctx.lineTo(-10, -44);
    ctx.lineTo(10, -44);
    ctx.lineTo(16, -60);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Glowing Neon Circuit Lines
    ctx.strokeStyle = '#00d2ff';
    ctx.lineWidth = 2.0;
    ctx.beginPath();
    ctx.moveTo(-9, -46);
    ctx.lineTo(-4, -34);
    ctx.lineTo(9, -34);
    ctx.stroke();

    // Tactical Cyber Belt
    ctx.fillStyle = this.INK_COLOR;
    ctx.fillRect(-14, -26, 28, 6);
    ctx.fillStyle = '#00d2ff';
    ctx.fillRect(-4, -27, 8, 8);
  }

  private static renderDragonTorsoMom(ctx: CanvasRenderingContext2D): void {
    // Golden Dragon-Wing Pauldrons
    ctx.fillStyle = '#d97706';
    ctx.strokeStyle = '#fef08a';
    ctx.lineWidth = 2.0;

    // Left pauldron
    ctx.beginPath();
    ctx.moveTo(-16, -56);
    ctx.lineTo(-24, -48);
    ctx.lineTo(-12, -44);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Right pauldron
    ctx.beginPath();
    ctx.moveTo(16, -56);
    ctx.lineTo(24, -48);
    ctx.lineTo(12, -44);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Dragon Scale Plates
    ctx.strokeStyle = '#fcd34d';
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.arc(0, -40, 7.5, 0, Math.PI, false);
    ctx.arc(0, -32, 8.5, 0, Math.PI, false);
    ctx.stroke();

    // Glowing Dragon Ruby Core
    ctx.fillStyle = '#ef4444';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(0, -44, 4.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
  }

  private static renderCasualTorsoMom(ctx: CanvasRenderingContext2D): void {
    // Chic Brooch & Heart Locket
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(-2.5, -42, 2.5, 0, Math.PI * 2);
    ctx.arc(2.5, -42, 2.5, 0, Math.PI * 2);
    ctx.fill();

    // Belt
    ctx.fillStyle = 'rgba(0, 0, 0, 0.18)';
    ctx.fillRect(-14, -26, 28, 5);
    ctx.fillStyle = '#fbbf24';
    ctx.beginPath();
    ctx.arc(0, -23.5, 3.8, 0, Math.PI * 2);
    ctx.fill();
  }

  private static renderCyberpunkTorsoDad(ctx: CanvasRenderingContext2D): void {
    // Heavy Armored Collar
    ctx.fillStyle = '#facc15';
    ctx.strokeStyle = '#ff2a85';
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.moveTo(-19, -64);
    ctx.lineTo(-12, -45);
    ctx.lineTo(12, -45);
    ctx.lineTo(19, -64);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Cyber Lines
    ctx.strokeStyle = '#00d2ff';
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    ctx.moveTo(-11, -46);
    ctx.lineTo(0, -36);
    ctx.lineTo(0, -22);
    ctx.stroke();

    // Neon Buckle
    ctx.fillStyle = '#ff2a85';
    ctx.fillRect(-5, -28, 10, 8);
  }

  private static renderDragonTorsoDad(ctx: CanvasRenderingContext2D): void {
    // Heavy Steel & Gold Pauldrons
    ctx.fillStyle = '#475569';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 2.2;

    // Left
    ctx.beginPath();
    ctx.moveTo(-20, -60);
    ctx.lineTo(-29, -48);
    ctx.lineTo(-14, -44);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Right
    ctx.beginPath();
    ctx.moveTo(20, -60);
    ctx.lineTo(29, -48);
    ctx.lineTo(14, -44);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Dragon Crest
    ctx.fillStyle = '#fbbf24';
    ctx.beginPath();
    ctx.arc(0, -40, 8.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#dc2626';
    ctx.beginPath();
    ctx.arc(0, -40, 4.8, 0, Math.PI * 2);
    ctx.fill();
  }

  private static renderCasualTorsoDad(ctx: CanvasRenderingContext2D): void {
    // Casual Graphic Print ("P2 / LOVE")
    ctx.fillStyle = 'rgba(255, 255, 255, 0.4)';
    ctx.beginPath();
    ctx.roundRect(-8, -44, 16, 11, 2.5);
    ctx.fill();
  }
}
