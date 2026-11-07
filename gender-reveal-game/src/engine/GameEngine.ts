/**
 * 🎮 GameEngine: Animated Co-op Cart Adventure & Gender Reveal Engine
 *
 * Implements the automated cinematic cartoon journey inspired by Super Mario & Sonic the Hedgehog:
 * - Parents (Mom & Dad) and Pet ride together in an arcade cart ("carrinho")
 * - 100% automated smooth animation sequence passing through dynamic obstacles & structures:
 *   1. 🚦 Start Line Countdown (3.. 2.. 1.. GO!)
 *   2. 🍄 Zone 1: Green Hill & Mushroom Valley (rolling hills, warp pipes, ? blocks, brick blocks, golden rings)
 *   3. 🌀 Zone 2: Sonic 360° Corkscrew Loop & Giant Rubber Ducky Spring Jump
 *   4. ⚡ Zone 3: Stardust Cyberpunk Bridge, Cloud Rocket Chasm & Neon Glitch Tunnel
 *   5. 🏰 Zone 4: Bowser's Volcanic Castle, Flaming Torches & Mega Mystery Block Supernova Smash
 *   6. 🏆 Zone 5 / Grand Finale: Mario End Flagpole, 1st Place Golden Podium & Baby Player 3 Reveal!
 * - Non-blocking user interaction: Screen tap / click honks cartoon horn (bibi!) & launches fireworks in finale!
 */

import { GameConfig, HobbyType, OutfitTheme } from '../config/gameConfig';
import { AudioEngine } from '../audio/AudioEngine';
import { ParallaxBackground } from './ParallaxBackground';
import { CharacterRenderer } from '../entities/CharacterRenderer';
import { CartRenderer, CartAnimationState } from '../entities/CartRenderer';
import { ParticleSystem } from '../entities/ParticleSystem';
import { WorldObjects, HobbyItem } from '../entities/WorldObjects';
import { HudOverlay } from '../ui/HudOverlay';
import { getTrackPoint, getVehiclePoint } from './TrackElevation';

export type GameState =
  | 'TITLE_SCREEN'
  | 'ANIM_COUNTDOWN'
  | 'ANIM_CRUISING'
  | 'ANIM_OBSTACLE_1_DUCKY'
  | 'ANIM_OBSTACLE_2_CLOUDS'
  | 'ANIM_OBSTACLE_3_TUNNEL'
  | 'ANIM_OBSTACLE_4_BLOCK'
  | 'GRAND_FINALE';

interface MarioPipeItem {
  x: number;
  height: number;
}

interface BlockItem {
  x: number;
  yOffset: number;
  type: 'question' | 'brick';
  hit: boolean;
  bounceY: number;
}

interface SonicRingItem {
  x: number;
  yOffset: number;
  collected: boolean;
}

interface DashPadItem {
  x: number;
}

interface TunnelArchItem {
  x: number;
}

interface CastleWallItem {
  x: number;
}

export class GameEngine {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private config: GameConfig;
  private audio: AudioEngine;
  private parallax: ParallaxBackground;
  private particles: ParticleSystem;
  private hud: HudOverlay;

  private currentState: GameState = 'TITLE_SCREEN';
  private stateTime: number = 0;
  private totalTime: number = 0;
  private cameraX: number = 0;
  private cameraShake: number = 0;

  // Cart Animation State
  private cartState: CartAnimationState;

  // Level Entities (Mario & Sonic Structures along 18000px world track)
  private hobbyItems: HobbyItem[] = [];
  private marioPipes: MarioPipeItem[] = [];
  private blocks: BlockItem[] = [];
  private sonicRings: SonicRingItem[] = [];
  private dashPads: DashPadItem[] = [];
  private tunnelArches: TunnelArchItem[] = [];
  private castleWalls: CastleWallItem[] = [];

  // Stage Specific Timers & Progress
  private countdownNumber: number = 3;
  private duckyJumpArc: number = 0; // 0 to 1
  private glitchFlashTimer: number = 0;
  private glitchText: string = '99% MENINO!';
  private blockImpacted: boolean = false;
  private whiteoutAlpha: number = 0;
  private finaleBabyY: number = 0;

  // Landmark World Coordinates
  private readonly loopCenterX = 5400;
  private readonly springRampX = 7400;
  private readonly duckyX = 7620;
  private readonly megaBlockX = 16800;
  private readonly flagpoleX = 17350;
  private readonly podiumX = 17700;

  // Viewport Dimensions
  private width: number = 1920;
  private height: number = 1080;
  private isRunning: boolean = false;

  constructor(canvas: HTMLCanvasElement, config: GameConfig, audio: AudioEngine) {
    this.canvas = canvas;
    const context = canvas.getContext('2d');
    if (!context) throw new Error('Could not get Canvas 2D context');
    this.ctx = context;

    this.config = config;
    this.audio = audio;
    this.parallax = new ParallaxBackground();
    this.particles = new ParticleSystem();
    this.hud = new HudOverlay(config);

    this.cartState = {
      x: 320,
      y: 800,
      vx: 0,
      vy: 0,
      tiltAngle: 0,
      bounceY: 0,
      squashStretchX: 1,
      squashStretchY: 1,
      wheelRotation: 0,
      isBoosting: false,
      isAirborne: false,
      eyeExpression: 'normal'
    };

    this.resize();
    window.addEventListener('resize', () => this.resize());
    this.initLevelItems();
  }

  public setConfig(newConfig: GameConfig): void {
    this.config = newConfig;
    this.hud.setConfig(newConfig);
  }

  public setOutfitTheme(theme: OutfitTheme): void {
    this.config.parents.mom.outfitTheme = theme;
    this.config.parents.dad.outfitTheme = theme;
    this.config.pet.outfitTheme = theme;

    if (theme === 'cyberpunk') {
      this.config.parents.mom.outfitColor = '#facc15';
      this.config.parents.dad.outfitColor = '#00d2ff';
    } else if (theme === 'dragon_knight') {
      this.config.parents.mom.outfitColor = '#dc2626';
      this.config.parents.dad.outfitColor = '#475569';
    } else {
      this.config.parents.mom.outfitColor = '#ec4899';
      this.config.parents.dad.outfitColor = '#3b82f6';
    }

    this.particles.spawnShockwave(this.cartState.x, this.cartState.y - 40, '#fbbf24', 160);
    this.audio.playCoin();
  }

  public getState(): GameState {
    return this.currentState;
  }

  public resize(): void {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    this.width = window.innerWidth;
    this.height = window.innerHeight;
    this.canvas.width = this.width * dpr;
    this.canvas.height = this.height * dpr;
    this.ctx.resetTransform?.();
    this.ctx.scale(dpr, dpr);

    const groundY = this.height - 110;
    this.cartState.y = groundY;
  }

  public start(): void {
    if (this.isRunning) return;
    this.isRunning = true;
    let lastTime = performance.now();

    const loop = (currentTime: number) => {
      if (!this.isRunning) return;
      const dt = Math.min((currentTime - lastTime) / 1000, 0.05);
      lastTime = currentTime;

      this.update(dt);
      this.render();

      requestAnimationFrame(loop);
    };

    requestAnimationFrame(loop);
  }

  /**
   * Screen tap / click or Space bar action.
   * If on title screen: starts animation.
   * During animation: honks cartoon horn (bibi!) with musical notes & sparks!
   * In grand finale: launches extra firework bursts!
   */
  public handleAction(): void {
    this.audio.init();

    if (this.currentState === 'TITLE_SCREEN') {
      this.startAnimation();
      return;
    }

    if (this.currentState === 'GRAND_FINALE') {
      this.triggerFinaleFireworks();
      return;
    }

    // Honk the cartoon horn during animation!
    this.audio.playCarHorn();
    this.particles.spawnFloatingText(this.cartState.x, this.cartState.y - 90, '📯 BI-BI! 🎵', '#fbbf24', 24);
    this.particles.spawnShockwave(this.cartState.x + 80, this.cartState.y, '#00d2ff', 80);
  }

  public startAnimation(): void {
    this.transitionTo('ANIM_COUNTDOWN');
  }

  public restartGame(): void {
    this.transitionTo('TITLE_SCREEN');
  }

  public skipToFinale(): void {
    this.cameraX = this.podiumX - this.width * 0.5;
    this.transitionTo('GRAND_FINALE');
  }

  public transitionTo(newState: GameState): void {
    this.currentState = newState;
    this.stateTime = 0;

    const groundY = this.height - 110;

    switch (newState) {
      case 'TITLE_SCREEN':
        this.audio.playBgm('title');
        this.cameraX = 0;
        this.cartState.x = Math.min(380, this.width * 0.35);
        this.cartState.y = groundY;
        this.cartState.vx = 0;
        this.cartState.vy = 0;
        this.cartState.tiltAngle = 0;
        this.cartState.isBoosting = false;
        this.cartState.isAirborne = false;
        this.cartState.eyeExpression = 'normal';
        this.whiteoutAlpha = 0;
        this.initLevelItems();
        break;

      case 'ANIM_COUNTDOWN':
        this.audio.playBgm('running');
        this.cameraX = 0;
        this.cartState.x = Math.min(320, this.width * 0.28);
        this.cartState.y = groundY;
        this.cartState.vx = 0;
        this.cartState.tiltAngle = 0;
        this.cartState.isBoosting = false;
        this.cartState.isAirborne = false;
        this.cartState.eyeExpression = 'happy';
        this.countdownNumber = 3;
        this.audio.playEngineRev();
        break;

      case 'ANIM_CRUISING':
        this.audio.playBgm('running');
        this.cartState.eyeExpression = 'normal';
        this.cartState.isBoosting = false;
        this.cartState.isAirborne = false;
        break;

      case 'ANIM_OBSTACLE_1_DUCKY':
        this.duckyJumpArc = 0;
        this.cartState.eyeExpression = 'normal';
        break;

      case 'ANIM_OBSTACLE_2_CLOUDS':
        this.cartState.eyeExpression = 'happy';
        break;

      case 'ANIM_OBSTACLE_3_TUNNEL':
        this.audio.playBgm('glitch');
        this.glitchFlashTimer = 0;
        this.glitchText = '99% MENINO!';
        this.cartState.eyeExpression = 'stars';
        break;

      case 'ANIM_OBSTACLE_4_BLOCK':
        this.audio.playBgm('climax');
        this.blockImpacted = false;
        this.cartState.eyeExpression = 'normal';
        break;

      case 'GRAND_FINALE':
        this.audio.playBgm('victory');
        this.cartState.vx = 0;
        this.cartState.tiltAngle = 0;
        this.cartState.isAirborne = false;
        this.cartState.isBoosting = false;
        this.cartState.eyeExpression = 'happy';
        this.finaleBabyY = -30;
        this.whiteoutAlpha = 1.0;
        this.triggerFinaleFireworks();
        break;
    }
  }

  // =========================================================================
  // ⚙️ UPDATE LOOP & TERRAIN / OBSTACLE COORDINATION
  // =========================================================================

  private update(dt: number): void {
    this.totalTime += dt;
    this.stateTime += dt;

    if (this.cameraShake > 0) {
      this.cameraShake = Math.max(0, this.cameraShake - dt * 25);
    }

    if (this.whiteoutAlpha > 0) {
      this.whiteoutAlpha = Math.max(0, this.whiteoutAlpha - dt * 1.5);
    }

    // Update block bounce returns
    for (const b of this.blocks) {
      if (b.bounceY < 0) {
        b.bounceY = Math.min(0, b.bounceY + dt * 45);
      }
    }

    const groundY = this.height - 110;
    const cartScreenX = Math.min(360, this.width * 0.28);
    this.cartState.x = cartScreenX;
    const cartWorldX = this.cameraX + cartScreenX;

    switch (this.currentState) {
      case 'TITLE_SCREEN':
        this.cartState.bounceY = Math.sin(this.totalTime * 8) * 1.5;
        this.cartState.wheelRotation = 0;
        this.cartState.y = groundY - 18 + this.cartState.bounceY;
        this.cartState.tiltAngle = 0;
        break;

      case 'ANIM_COUNTDOWN':
        this.updateCountdown(dt);
        break;

      case 'ANIM_CRUISING':
      case 'ANIM_OBSTACLE_1_DUCKY':
      case 'ANIM_OBSTACLE_2_CLOUDS':
      case 'ANIM_OBSTACLE_3_TUNNEL':
      case 'ANIM_OBSTACLE_4_BLOCK':
        this.updateMotionAndWorldPhysics(dt, groundY, cartWorldX, cartScreenX);
        break;

      case 'GRAND_FINALE':
        this.updateGrandFinale(dt, groundY);
        break;
    }

    this.particles.update(dt);
  }

  private updateCountdown(_dt: number): void {
    this.cartState.bounceY = Math.sin(this.totalTime * 18) * 2.5; // Engine rev shake
    const groundY = this.height - 110;
    this.cartState.y = groundY - 18 + this.cartState.bounceY;
    this.cartState.tiltAngle = 0;

    if (this.stateTime < 1.0) {
      this.countdownNumber = 3;
    } else if (this.stateTime < 2.0) {
      this.countdownNumber = 2;
    } else if (this.stateTime < 2.8) {
      this.countdownNumber = 1;
    } else {
      this.countdownNumber = 0; // GO!
    }

    if (this.stateTime >= 3.2) {
      this.transitionTo('ANIM_CRUISING');
    }
  }

  private updateMotionAndWorldPhysics(
    dt: number,
    groundY: number,
    cartWorldX: number,
    cartScreenX: number
  ): void {
    // -----------------------------------------------------------------
    // 1. DYNAMIC SPEED COMPUTATION BY TRACK REGION & OBSTACLE
    // -----------------------------------------------------------------
    let targetSpeed = 540; // Default cruising speed

    if (cartWorldX < 4500) {
      // Zone 1: Green Hill acceleration
      targetSpeed = Math.min(540, 200 + this.stateTime * 140);
    } else if (cartWorldX >= 4800 && cartWorldX < 5800) {
      // Sonic Loop: high speed dash
      targetSpeed = 620;
    } else if (cartWorldX >= 7000 && cartWorldX < 7380) {
      // Approaching Giant Rubber Ducky: screeching brakes
      targetSpeed = 400;
      if (cartWorldX >= 7100 && cartWorldX < 7180 && this.stateTime % 1.0 < 0.1) {
        this.audio.playTireScreech();
      }
    } else if (cartWorldX >= 7380 && cartWorldX < 7860) {
      // Super Spring Jump over Ducky
      targetSpeed = 500;
    } else if (cartWorldX >= 10300 && cartWorldX < 11600) {
      // Rocket Nitro Boost across Cloud Chasm
      targetSpeed = 700;
    } else if (cartWorldX >= 12000 && cartWorldX < 13600) {
      // Cyber Glitch Tunnel
      targetSpeed = 620;
    } else if (cartWorldX >= 16200 && cartWorldX < 16800) {
      // Approaching Mega Mystery Block: dramatic cinematic slow-motion!
      const approachP = (cartWorldX - 16200) / 600;
      targetSpeed = 480 - approachP * 260; // Slows down to 220 px/s
    } else if (cartWorldX >= 16800 && cartWorldX < 17700) {
      // After explosion: cruising to finish line and podium
      targetSpeed = 460;
    }

    // Advance camera smoothly
    this.cameraX += targetSpeed * dt;
    this.cartState.wheelRotation += targetSpeed * dt * 0.05;
    this.cartState.bounceY = Math.sin(this.totalTime * 14) * 1.8;

    // -----------------------------------------------------------------
    // 2. TRACK ELEVATION & TILT EVALUATION (MARIO & SONIC PHYSICS)
    // -----------------------------------------------------------------
    const vehPt = getVehiclePoint(cartWorldX, groundY);
    this.cartState.y = vehPt.y - 18 + this.cartState.bounceY;
    this.cartState.tiltAngle = vehPt.slope;
    this.cartState.isAirborne = vehPt.isAirborne;

    // -----------------------------------------------------------------
    // 3. SPECIAL STAGE EVENTS & TRIGGER DETECTION
    // -----------------------------------------------------------------

    // A. Sonic Loop Entry & Exit (5200 -> 5600)
    if (cartWorldX >= 5200 && cartWorldX <= 5600) {
      this.cartState.eyeExpression = 'happy';
      if (Math.random() < 0.3) {
        this.particles.spawnSparkles(cartScreenX + 40, this.cartState.y, '#facc15', 3);
      }
    }

    // B. Super Spring Ramp Bounce at 7400
    if (cartWorldX >= 7390 && cartWorldX <= 7440 && this.duckyJumpArc === 0) {
      this.duckyJumpArc = 1;
      this.audio.playSpringBounce();
      this.cameraShake = 10;
      this.cartState.eyeExpression = 'shocked';
      this.particles.spawnShockwave(cartScreenX, groundY, '#fbbf24', 160);
      this.particles.spawnFloatingText(cartScreenX, groundY - 140, 'BOING! ★ MEGA SALTO!', '#fbbf24', 28);
    }

    // Landing from Ducky Jump at 7820
    if (cartWorldX >= 7820 && cartWorldX <= 7860 && this.duckyJumpArc === 1) {
      this.duckyJumpArc = 2; // Marked landed
      this.audio.playCoin();
      this.cameraShake = 8;
      this.cartState.eyeExpression = 'happy';
      this.particles.spawnShockwave(cartScreenX, groundY, '#38bdf8', 120);
      this.particles.spawnFloatingText(cartScreenX, groundY - 80, 'POUSO PERFEITO! 🚀', '#38bdf8', 24);
    }

    // C. Rocket Nitro Boost Trigger at 10400
    if (cartWorldX >= 10400 && cartWorldX <= 11500) {
      if (!this.cartState.isBoosting) {
        this.cartState.isBoosting = true;
        this.audio.playRocketBoost();
        this.cameraShake = 12;
        this.particles.spawnShockwave(cartScreenX, groundY, '#00d2ff', 180);
        this.particles.spawnFloatingText(cartScreenX, groundY - 120, '🔥 ROCKET NITRO BOOST! 🔥', '#00d2ff', 26);
      }
      if (Math.random() < 0.45) {
        this.particles.spawnShockwave(cartScreenX - 70, this.cartState.y + 10, '#00d2ff', 35);
      }
    } else if (this.cartState.isBoosting && cartWorldX > 11500) {
      this.cartState.isBoosting = false;
    }

    // D. Cyber Glitch Hologram Updates (12000 -> 13600)
    if (cartWorldX >= 12000 && cartWorldX < 13600) {
      this.glitchFlashTimer += dt;
      if (this.glitchFlashTimer >= 1.1) {
        this.glitchFlashTimer = 0;
        this.audio.playGlitchZap();
        this.cameraShake = 5;
        this.glitchText = this.glitchText.includes('MENINO') ? '99% MENINA!' : '99% MENINO!';
      }
    }

    // E. Mega Mystery Block Smash Explosion at 16800
    if (cartWorldX >= 16780 && !this.blockImpacted) {
      this.blockImpacted = true;
      this.cameraShake = 32;
      this.audio.playBlockHit(4);
      this.audio.playFirework();
      this.whiteoutAlpha = 1.0;

      // Massive comic-book explosion shockwaves & confetti
      this.particles.spawnShockwave(cartScreenX + 80, groundY - 60, '#fbbf24', 300);
      this.particles.spawnShockwave(cartScreenX + 80, groundY - 60, '#ffffff', 340);
      this.particles.spawnFloatingText(cartScreenX + 80, groundY - 160, '💥 KABOOM! REVELAÇÃO! 💥', '#fbbf24', 38);
    }

    // F. Reaching Grand Finale Podium at 17700
    if (cartWorldX >= this.podiumX) {
      this.transitionTo('GRAND_FINALE');
      return;
    }

    // -----------------------------------------------------------------
    // 4. LEVEL ENTITY INTERACTIONS (RINGS, BLOCKS, TOKENS, PADS)
    // -----------------------------------------------------------------

    // Sonic Golden Rings
    for (const ring of this.sonicRings) {
      if (!ring.collected && Math.abs(cartWorldX - ring.x) < 48) {
        ring.collected = true;
        this.audio.playCoin();
        const rx = ring.x - this.cameraX;
        const ry = groundY - ring.yOffset;
        this.particles.spawnSparkles(rx, ry, '#fbbf24', 8);
        this.particles.spawnFloatingText(rx, ry - 20, '+10 ★', '#fbbf24', 16);
      }
    }

    // Family Hobby Tokens
    for (const item of this.hobbyItems) {
      if (!item.collected && Math.abs(cartWorldX - item.x) < 55) {
        item.collected = true;
        this.audio.playCoin();
        const ix = item.x - this.cameraX;
        this.particles.spawnShockwave(ix, item.y, '#38bdf8', 75);
        this.particles.spawnFloatingText(ix, item.y - 40, '+100 ★', '#fbbf24', 22);
      }
    }

    // Question & Brick Blocks
    for (const block of this.blocks) {
      if (!block.hit && Math.abs(cartWorldX - block.x) < 38) {
        block.hit = true;
        block.bounceY = -14;
        this.audio.playBlockHit(1);
        const bx = block.x - this.cameraX;
        const by = groundY - block.yOffset;
        this.particles.spawnFloatingText(bx, by - 36, block.type === 'question' ? '★ ITEM!' : '💥', '#fbbf24', 18);
        this.particles.spawnSparkles(bx, by, '#fbbf24', 6);
      }
    }

    // Speed Dash Pads
    for (const pad of this.dashPads) {
      if (Math.abs(cartWorldX - pad.x) < 26) {
        this.audio.playEngineRev();
        this.particles.spawnShockwave(pad.x - this.cameraX, groundY, '#facc15', 70);
      }
    }

    // -----------------------------------------------------------------
    // 5. UPDATE GAME STATE BASED ON TRACK WORLD X
    // -----------------------------------------------------------------
    if (cartWorldX < 4500 && this.currentState !== 'ANIM_CRUISING') {
      this.currentState = 'ANIM_CRUISING';
    } else if (cartWorldX >= 4500 && cartWorldX < 9000 && this.currentState !== 'ANIM_OBSTACLE_1_DUCKY') {
      this.currentState = 'ANIM_OBSTACLE_1_DUCKY';
    } else if (cartWorldX >= 9000 && cartWorldX < 11800 && this.currentState !== 'ANIM_OBSTACLE_2_CLOUDS') {
      this.currentState = 'ANIM_OBSTACLE_2_CLOUDS';
    } else if (cartWorldX >= 11800 && cartWorldX < 14200 && this.currentState !== 'ANIM_OBSTACLE_3_TUNNEL') {
      this.currentState = 'ANIM_OBSTACLE_3_TUNNEL';
    } else if (cartWorldX >= 14200 && cartWorldX < this.podiumX && this.currentState !== 'ANIM_OBSTACLE_4_BLOCK') {
      this.currentState = 'ANIM_OBSTACLE_4_BLOCK';
    }
  }

  private updateGrandFinale(_dt: number, groundY: number): void {
    this.cartState.y = groundY - 22;
    this.cartState.tiltAngle = 0;
    this.cartState.bounceY = Math.sin(this.totalTime * 4) * 2;
    this.finaleBabyY = Math.sin(this.totalTime * 3) * 6;

    // Keep camera centered on winner podium
    const targetCamX = this.podiumX - this.width * 0.5;
    this.cameraX += (targetCamX - this.cameraX) * 0.1;

    // Spontaneous celebratory fireworks
    if (Math.random() < 0.12) {
      const color = this.config.revealGender === 'boy' ? this.config.themeColors.boyPrimary : this.config.themeColors.girlPrimary;
      this.particles.spawnFireworkBurst(
        this.width * 0.15 + Math.random() * (this.width * 0.7),
        this.height * 0.12 + Math.random() * (this.height * 0.35),
        color,
        this.config.themeColors.arcadeGold
      );
      this.audio.playFirework();
    }
  }

  private triggerFinaleFireworks(): void {
    const color = this.config.revealGender === 'boy' ? this.config.themeColors.boyPrimary : this.config.themeColors.girlPrimary;
    for (let i = 0; i < 4; i++) {
      this.particles.spawnFireworkBurst(
        this.width * 0.2 + i * (this.width * 0.2),
        this.height * 0.15 + Math.random() * (this.height * 0.25),
        color,
        this.config.themeColors.arcadeGold
      );
    }
    this.audio.playFirework();
  }

  // =========================================================================
  // 🎨 RENDER PIPELINE (WITH TWO-PASS LOOP DEPTH)
  // =========================================================================

  private render(): void {
    this.ctx.clearRect(0, 0, this.width, this.height);

    this.ctx.save();
    if (this.cameraShake > 0) {
      const sx = (Math.random() - 0.5) * this.cameraShake;
      const sy = (Math.random() - 0.5) * this.cameraShake;
      this.ctx.translate(sx, sy);
    }

    const groundY = this.height - 110;

    // 1. Dynamic 5-Layer Parallax Universe
    this.parallax.render(this.ctx, this.width, this.height, this.cameraX, this.totalTime);

    // 2. World Interactive Entities - BACKGROUND PASS
    // Renders pipes, blocks, rings, ducky, and back half of Sonic Loop
    this.renderWorldEntities(groundY, 'background');

    // 3. The Co-op Adventure Cart & Passengers (Mom, Dad, Pet)
    if (this.currentState !== 'GRAND_FINALE') {
      CartRenderer.renderCart(
        this.ctx,
        this.config.parents.mom,
        this.config.parents.dad,
        this.config.pet,
        this.cartState,
        1.15
      );
    } else {
      this.renderGrandFinaleScene(groundY);
    }

    // 4. World Interactive Entities - FOREGROUND PASS
    // Renders front half of Sonic Loop (overlaps cart when looping!)
    this.renderWorldEntities(groundY, 'foreground');

    // 5. Particles & Floating Texts
    this.particles.render(this.ctx);

    // 6. Whiteout Flash on Climax detonation
    if (this.whiteoutAlpha > 0) {
      this.ctx.fillStyle = `rgba(255, 255, 255, ${this.whiteoutAlpha})`;
      this.ctx.fillRect(0, 0, this.width, this.height);
    }

    this.ctx.restore();

    // 7. UI Overlays (Scrubber, Banner, Countdown)
    this.renderUiOverlays();
  }

  private renderWorldEntities(groundY: number, pass: 'background' | 'foreground'): void {
    const loopScreenX = this.loopCenterX - this.cameraX;

    // -------------------------------------------------------------
    // FOREGROUND PASS: Only the front crossover rail of Sonic Loop
    // -------------------------------------------------------------
    if (pass === 'foreground') {
      if (loopScreenX >= -300 && loopScreenX <= this.width + 300) {
        WorldObjects.renderSonicLoop(this.ctx, loopScreenX, groundY, 120, true);
      }
      return;
    }

    // -------------------------------------------------------------
    // BACKGROUND PASS: All structures behind the vehicle
    // -------------------------------------------------------------

    // 1. Sonic 360° Loop (Back half)
    if (loopScreenX >= -300 && loopScreenX <= this.width + 300) {
      WorldObjects.renderSonicLoop(this.ctx, loopScreenX, groundY, 120, false);
    }

    // 2. Mario Warp Pipes
    for (const pipe of this.marioPipes) {
      const px = pipe.x - this.cameraX;
      if (px >= -100 && px <= this.width + 100) {
        const pt = getTrackPoint(pipe.x, groundY);
        WorldObjects.renderGreenPipe(this.ctx, px, pt.y, 60, pipe.height);
      }
    }

    // 3. Question & Brick Blocks
    for (const b of this.blocks) {
      const bx = b.x - this.cameraX;
      if (bx >= -80 && bx <= this.width + 80) {
        const pt = getTrackPoint(b.x, groundY);
        const by = pt.y - b.yOffset + b.bounceY;
        if (b.type === 'question') {
          WorldObjects.renderQuestionBlock(this.ctx, bx, by, 46, this.totalTime);
        } else {
          WorldObjects.renderBrickBlock(this.ctx, bx, by, 46);
        }
      }
    }

    // 4. Sonic Golden Rings
    for (const ring of this.sonicRings) {
      if (ring.collected) continue;
      const rx = ring.x - this.cameraX;
      if (rx >= -60 && rx <= this.width + 60) {
        const pt = getTrackPoint(ring.x, groundY);
        const ry = pt.y - ring.yOffset;
        WorldObjects.renderSonicRing(this.ctx, rx, ry, this.totalTime);
      }
    }

    // 5. Speed Dash Pads
    for (const pad of this.dashPads) {
      const px = pad.x - this.cameraX;
      if (px >= -120 && px <= this.width + 120) {
        const pt = getTrackPoint(pad.x, groundY);
        WorldObjects.renderSpeedDashPad(this.ctx, px, pt.y + 4, 80, this.totalTime);
      }
    }

    // 6. Cruising Hobby Tokens
    for (const item of this.hobbyItems) {
      WorldObjects.renderHobbyItem(this.ctx, item, this.cameraX, this.totalTime);
    }

    // 7. Obstacle 1: Super Spring Ramp & Giant Rubber Ducky
    const rampScreenX = this.springRampX - this.cameraX;
    if (rampScreenX >= -160 && rampScreenX <= this.width + 160) {
      WorldObjects.renderSpringRamp(this.ctx, rampScreenX, groundY, this.duckyJumpArc === 1);
    }

    const duckyScreenX = this.duckyX - this.cameraX;
    if (duckyScreenX >= -240 && duckyScreenX <= this.width + 240) {
      WorldObjects.renderGiantRubberDucky(this.ctx, duckyScreenX, groundY, this.totalTime);
    }

    // 8. Obstacle 2: Storm Clouds in the Chasm
    const cloudPositions = [10750, 10950, 11150, 11350];
    for (const cx of cloudPositions) {
      const sx = cx - this.cameraX;
      if (sx >= -160 && sx <= this.width + 160) {
        WorldObjects.renderStormCloud(
          this.ctx,
          { x: sx, y: groundY - 140, width: 140, height: 60, passed: false, label: 'NUVEM DO TIO DORMILHÃO' },
          this.totalTime
        );
      }
    }

    // 9. Obstacle 3: Cyberpunk Neon Tunnel Arches
    for (const arch of this.tunnelArches) {
      const ax = arch.x - this.cameraX;
      if (ax >= -160 && ax <= this.width + 160) {
        WorldObjects.renderNeonTunnelArch(this.ctx, ax, groundY - 35, 180, 220, this.totalTime);
      }
    }

    // 10. Obstacle 4: Bowser Castle Battlements & Torches
    for (const wall of this.castleWalls) {
      const wx = wall.x - this.cameraX;
      if (wx >= -160 && wx <= this.width + 160) {
        WorldObjects.renderCastleBattlements(this.ctx, wx, groundY, 150, 110, this.totalTime);
      }
    }

    // 11. Mega Mystery Block [ ? ]
    const megaBlockScreenX = this.megaBlockX - this.cameraX;
    if (megaBlockScreenX >= -200 && megaBlockScreenX <= this.width + 200) {
      WorldObjects.renderMegaMysteryBlock(
        this.ctx,
        megaBlockScreenX,
        groundY - 80,
        this.blockImpacted ? 4 : 0,
        true,
        this.totalTime
      );
    }

    // 12. Mario Goal Flagpole
    const flagScreenX = this.flagpoleX - this.cameraX;
    if (flagScreenX >= -100 && flagScreenX <= this.width + 100) {
      WorldObjects.renderMarioFlagpole(this.ctx, flagScreenX, groundY, 200, this.totalTime);
    }

    // 13. Winner's 1st Place Podium
    const podScreenX = this.podiumX - this.cameraX;
    if (podScreenX >= -240 && podScreenX <= this.width + 240) {
      WorldObjects.renderWinnerPodium(this.ctx, podScreenX, groundY, this.totalTime);
    }
  }

  private renderGrandFinaleScene(groundY: number): void {
    const podScreenX = this.podiumX - this.cameraX;

    // 1. Winner's 1st Place Golden Podium
    WorldObjects.renderWinnerPodium(this.ctx, podScreenX, groundY, this.totalTime);

    // 2. The Cart parked proudly on the podium
    const parkedState: CartAnimationState = {
      ...this.cartState,
      x: podScreenX,
      y: groundY - 22,
      tiltAngle: 0,
      isAirborne: false,
      isBoosting: false,
      bounceY: Math.sin(this.totalTime * 4) * 2,
      eyeExpression: 'happy'
    };

    CartRenderer.renderCart(
      this.ctx,
      this.config.parents.mom,
      this.config.parents.dad,
      this.config.pet,
      parkedState,
      1.15
    );

    // 3. Mom & Dad triumphantly holding BABY PLAYER 3 with crown!
    const babyX = podScreenX;
    const babyY = groundY - 110 + this.finaleBabyY;

    CharacterRenderer.renderBabyPlayer3(
      this.ctx,
      this.config.revealGender,
      babyX,
      babyY,
      1.35,
      this.config.themeColors
    );
  }

  private renderUiOverlays(): void {
    if (this.currentState === 'TITLE_SCREEN') {
      this.hud.renderTitleScreen(this.ctx, this.width, this.height, this.totalTime);
      return;
    }

    if (this.currentState === 'GRAND_FINALE') {
      this.hud.renderRevealBanner(this.ctx, this.width, this.height, this.config.revealGender, this.totalTime);
      return;
    }

    // Top Animated Adventure Scrubber & Current Obstacle Alert
    const progress = Math.max(0, Math.min(1, (this.cameraX + this.cartState.x) / this.podiumX));
    const { title, subtitle } = this.getCurrentObstacleInfo();

    this.hud.renderAnimationScrubber(this.ctx, this.width, progress, title, subtitle);

    // Countdown Overlay (3.. 2.. 1.. GO!)
    if (this.currentState === 'ANIM_COUNTDOWN') {
      this.renderTrafficLightCountdown();
    }
  }

  private getCurrentObstacleInfo(): { title: string; subtitle?: string } {
    const isPt = this.config.language === 'pt';
    const cartWorldX = this.cameraX + this.cartState.x;

    if (this.currentState === 'ANIM_COUNTDOWN') {
      return {
        title: isPt ? '🚦 CONTAGEM REGRESSIVA PARA A LARGADA!' : '🚦 COUNTDOWN TO START!',
        subtitle: isPt ? 'Apertem os cintos!' : 'Buckle up!'
      };
    }

    if (cartWorldX < 4500) {
      return {
        title: isPt ? '🍄 ZONA 1: COLINAS VERDES & TUBOS DO MARIO' : '🍄 ZONE 1: GREEN HILL & MARIO PIPES',
        subtitle: isPt ? 'Colete moedas e itens da família!' : 'Collect rings and family items!'
      };
    }

    if (cartWorldX < 6000) {
      return {
        title: isPt ? '🌀 ZONA 2: O LOOP 360° DO SONIC!' : '🌀 ZONE 2: SONIC 360° LOOP-DE-LOOP!',
        subtitle: isPt ? 'De cabeça para baixo na super velocidade!' : 'Upside down at hyper speed!'
      };
    }

    if (cartWorldX < 9000) {
      return {
        title: isPt ? '🦆 OBSTÁCULO 1: O PATO GIGANTE E A SUPER MOLA!' : '🦆 OBSTACLE 1: GIANT DUCKY & SPRING RAMP!',
        subtitle: isPt ? 'Salto acrobático 360° no ar!' : '360° acrobatic air stunt!'
      };
    }

    if (cartWorldX < 11800) {
      return {
        title: isPt ? '☁️ OBSTÁCULO 2: SALTO COM FOGUETE NAS NUVENS!' : '☁️ OBSTACLE 2: ROCKET BOOST CLOUD JUMP!',
        subtitle: isPt ? 'Nitro turbo acionado sobre o abismo!' : 'Nitro rocket across the chasm!'
      };
    }

    if (cartWorldX < 14200) {
      return {
        title: isPt ? `⚡ OBSTÁCULO 3: TÚNEL GLITCH - ${this.glitchText}` : `⚡ OBSTACLE 3: GLITCH TUNNEL - ${this.glitchText}`,
        subtitle: isPt ? 'Sobrecarga de amor a 100%!' : '100% Love Overload!'
      };
    }

    return {
      title: isPt ? '❓ OBSTÁCULO 4: O MEGA BLOCO DE REVELAÇÃO!' : '❓ OBSTACLE 4: THE MEGA REVEAL BLOCK!',
      subtitle: isPt ? 'Impacto iminente em câmera lenta!' : 'Slow-mo impact imminent!'
    };
  }

  private renderTrafficLightCountdown(): void {
    const ctx = this.ctx;
    ctx.save();
    const cx = this.width / 2;
    const cy = this.height * 0.38;

    // Traffic light housing
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = '#475569';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.roundRect(cx - 50, cy - 110, 100, 220, 24);
    ctx.fill();
    ctx.stroke();

    // 3 Lamps (Red, Yellow, Green)
    const lampRadius = 26;
    const lamps = [
      { y: cy - 60, color: '#ef4444', active: this.countdownNumber === 3 || this.countdownNumber === 2 },
      { y: cy, color: '#f59e0b', active: this.countdownNumber === 1 },
      { y: cy + 60, color: '#22c55e', active: this.countdownNumber === 0 }
    ];

    lamps.forEach((lamp) => {
      ctx.fillStyle = lamp.active ? lamp.color : '#1e293b';
      ctx.strokeStyle = lamp.active ? '#ffffff' : '#334155';
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      ctx.arc(cx, lamp.y, lampRadius, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    });

    // Big Countdown Text Underneath
    ctx.font = '900 64px "Impact", "Arial Black", sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 8;

    const label = this.countdownNumber > 0 ? `${this.countdownNumber}` : 'GO! 🚀';
    const labelColor = this.countdownNumber > 0 ? '#fbbf24' : '#22c55e';

    ctx.strokeText(label, cx, cy + 160);
    ctx.fillStyle = labelColor;
    ctx.fillText(label, cx, cy + 160);

    ctx.restore();
  }

  private initLevelItems(): void {
    // 1. Family Hobby Tokens
    this.hobbyItems = [];
    const hobbies: HobbyType[] = ['gamepad', 'dumbbell', 'soccer', 'music', 'coffee', 'book', 'camera'];
    const hobbyXs = [950, 1500, 2300, 3100, 3800, 6300, 6800, 8500, 9700, 12500, 13100, 15300];
    hobbyXs.forEach((x, i) => {
      this.hobbyItems.push({
        x,
        y: this.height - 180 - (i % 3) * 20,
        type: hobbies[i % hobbies.length],
        collected: false,
        bobOffset: (i * Math.PI) / 3
      });
    });

    // 2. Mario Warp Pipes
    this.marioPipes = [
      { x: 850, height: 65 },
      { x: 1650, height: 85 },
      { x: 2750, height: 70 },
      { x: 3900, height: 95 },
      { x: 6100, height: 75 },
      { x: 8100, height: 80 }
    ];

    // 3. Question & Brick Blocks
    this.blocks = [
      // Zone 1 Formation 1
      { x: 1100, yOffset: 145, type: 'brick', hit: false, bounceY: 0 },
      { x: 1160, yOffset: 145, type: 'question', hit: false, bounceY: 0 },
      { x: 1220, yOffset: 145, type: 'question', hit: false, bounceY: 0 },
      { x: 1280, yOffset: 145, type: 'brick', hit: false, bounceY: 0 },
      // Zone 1 Formation 2
      { x: 2100, yOffset: 155, type: 'brick', hit: false, bounceY: 0 },
      { x: 2160, yOffset: 155, type: 'question', hit: false, bounceY: 0 },
      { x: 2220, yOffset: 155, type: 'brick', hit: false, bounceY: 0 },
      { x: 2280, yOffset: 155, type: 'question', hit: false, bounceY: 0 },
      { x: 2340, yOffset: 155, type: 'brick', hit: false, bounceY: 0 },
      // Zone 1 Formation 3
      { x: 3200, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 3260, yOffset: 150, type: 'question', hit: false, bounceY: 0 },
      { x: 3320, yOffset: 150, type: 'question', hit: false, bounceY: 0 },
      { x: 3380, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      // Zone 2
      { x: 6450, yOffset: 145, type: 'question', hit: false, bounceY: 0 },
      { x: 6510, yOffset: 145, type: 'question', hit: false, bounceY: 0 },
      { x: 6570, yOffset: 145, type: 'brick', hit: false, bounceY: 0 },
      { x: 8350, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 8410, yOffset: 150, type: 'question', hit: false, bounceY: 0 },
      { x: 8470, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      // Zone 4 Castle Rows
      { x: 14300, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 14360, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 15000, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 15060, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 15800, yOffset: 150, type: 'brick', hit: false, bounceY: 0 },
      { x: 15860, yOffset: 150, type: 'brick', hit: false, bounceY: 0 }
    ];

    // 4. Sonic Golden Spinning Rings
    this.sonicRings = [];
    const ringPositions = [
      // Wave 1
      { x: 520, y: 70 }, { x: 600, y: 80 }, { x: 680, y: 85 }, { x: 760, y: 80 },
      // Wave 2
      { x: 1350, y: 75 }, { x: 1420, y: 75 }, { x: 1490, y: 75 },
      // Wave 3
      { x: 1820, y: 80 }, { x: 1890, y: 85 }, { x: 1960, y: 80 },
      // Wave 4
      { x: 2450, y: 75 }, { x: 2520, y: 75 }, { x: 2590, y: 75 },
      // Wave 5
      { x: 3550, y: 80 }, { x: 3630, y: 85 }, { x: 3710, y: 80 },
      // Wave 6 (Entering Sonic Loop)
      { x: 4950, y: 75 }, { x: 5030, y: 85 }, { x: 5110, y: 95 },
      // Wave 7 (Inside Sonic Loop Crest)
      { x: 5320, y: 160 }, { x: 5400, y: 220 }, { x: 5480, y: 160 },
      // Wave 8 (Spring Jump Apex above Ducky)
      { x: 7500, y: 140 }, { x: 7570, y: 210 }, { x: 7640, y: 220 }, { x: 7710, y: 160 },
      // Wave 9 (Rocket Chasm Cloud Glide)
      { x: 10800, y: 130 }, { x: 10950, y: 160 }, { x: 11100, y: 160 }, { x: 11250, y: 130 },
      // Wave 10 (Cyber Glitch Tunnel)
      { x: 12200, y: 85 }, { x: 12600, y: 85 }, { x: 13000, y: 85 }
    ];

    for (const r of ringPositions) {
      this.sonicRings.push({
        x: r.x,
        yOffset: r.y,
        collected: false
      });
    }

    // 5. Speed Dash Pads (>>>)
    this.dashPads = [
      { x: 4850 },
      { x: 7150 },
      { x: 10400 },
      { x: 12050 }
    ];

    // 6. Neon Tunnel Arches
    this.tunnelArches = [
      { x: 12200 },
      { x: 12600 },
      { x: 13000 },
      { x: 13400 }
    ];

    // 7. Bowser Castle Walls & Battlements
    this.castleWalls = [
      { x: 14200 },
      { x: 14900 },
      { x: 15600 },
      { x: 16300 }
    ];
  }
}
