/**
 * 📦 WorldObjects: Standardized Full HD Collectibles, Obstacles & Podiums
 *
 * Sized and standardized for Full HD (1920x1080):
 * - Standard 52px arcade tokens with high-detail vector iconography
 * - Standard 40px energy orbs with glowing cores
 * - Giant Sunglasses Rubber Ducky obstacle
 * - Arcade Super Spring Ramp (Super Mola 🌀)
 * - Storm Clouds with sleeping cartoon eyes
 * - Mega Mystery Block [ ? ] with progressive cracks
 * - Grand Reveal 1st Place Winner's Podium & Checkered Flags
 */

import { HobbyType } from '../config/gameConfig';

export interface HobbyItem {
  x: number; // World X
  y: number; // World Y
  type: HobbyType;
  collected: boolean;
  bobOffset: number;
}

export interface EnergyOrb {
  x: number;
  y: number;
  gender: 'boy' | 'girl';
  collected: boolean;
  bobOffset: number;
}

export interface CloudObstacle {
  x: number;
  y: number;
  width: number;
  height: number;
  passed: boolean;
  label: string;
}

export interface FallingBabyItem {
  x: number;
  y: number;
  vy: number;
  type: 'bottle' | 'pacifier' | 'bear' | 'heart';
  collected: boolean;
  color: string;
  size: number;
}

export class WorldObjects {
  /**
   * Renders a standardized 52px circular collectible Hobby Item.
   */
  public static renderHobbyItem(
    ctx: CanvasRenderingContext2D,
    item: HobbyItem,
    cameraX: number,
    time: number
  ): void {
    if (item.collected) return;
    const screenX = item.x - cameraX;
    if (screenX < -60 || screenX > ctx.canvas.width + 60) return;

    ctx.save();
    const bobY = Math.sin(time * 4 + item.bobOffset) * 8;
    ctx.translate(screenX, item.y + bobY);

    const radius = 24;

    // Outer Neon Ring
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.arc(0, 0, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Inner Accent Ring
    ctx.strokeStyle = '#f59e0b';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(0, 0, radius - 4, 0, Math.PI * 2);
    ctx.stroke();

    // Standardized vector icon inside
    this.renderHobbyIcon(ctx, item.type);

    ctx.restore();
  }

  /**
   * Renders a standardized 40px Team Energy Orb.
   */
  public static renderEnergyOrb(
    ctx: CanvasRenderingContext2D,
    orb: EnergyOrb,
    cameraX: number,
    time: number
  ): void {
    if (orb.collected) return;
    const screenX = orb.x - cameraX;
    if (screenX < -60 || screenX > ctx.canvas.width + 60) return;

    ctx.save();
    const bobY = Math.sin(time * 5 + orb.bobOffset) * 6;
    ctx.translate(screenX, orb.y + bobY);

    const radius = 18;
    const color = orb.gender === 'boy' ? '#00d2ff' : '#ff2a85';

    // Outer Sphere
    ctx.fillStyle = color;
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.arc(0, 0, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Inner Glowing Core
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(-radius * 0.3, -radius * 0.3, radius * 0.35, 0, Math.PI * 2);
    ctx.fill();

    // Orbiting Electron/Particle Ring
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2;
    ctx.save();
    ctx.rotate(time * 3);
    ctx.beginPath();
    ctx.ellipse(0, 0, radius + 6, 8, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();

    ctx.restore();
  }

  /**
   * Renders the Giant Sunglasses Rubber Ducky obstacle on the road.
   */
  public static renderGiantRubberDucky(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    time: number
  ): void {
    ctx.save();
    ctx.translate(x, y - 20);

    const wobble = Math.sin(time * 5) * 0.08;
    const squish = 1 + Math.sin(time * 6) * 0.04;
    ctx.scale(squish, 2 - squish);
    ctx.rotate(wobble);

    // Ground Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.28)';
    ctx.beginPath();
    ctx.ellipse(0, 20, 56, 12, 0, 0, Math.PI * 2);
    ctx.fill();

    // Body
    ctx.fillStyle = '#fde047';
    ctx.strokeStyle = '#ca8a04';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.ellipse(0, 0, 54, 36, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Tail
    ctx.beginPath();
    ctx.moveTo(-45, -10);
    ctx.lineTo(-68, -28);
    ctx.lineTo(-38, 10);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Head
    ctx.beginPath();
    ctx.arc(32, -32, 30, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Beak (Orange)
    ctx.fillStyle = '#f97316';
    ctx.strokeStyle = '#c2410c';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.ellipse(58, -26, 20, 11, 0.1, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Cool Sunglasses 😎
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(24, -40, 18, 14);
    ctx.fillRect(48, -40, 18, 14);
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(16, -34);
    ctx.lineTo(68, -34);
    ctx.stroke();

    // Red Bowtie 🎀
    ctx.fillStyle = '#ef4444';
    ctx.strokeStyle = '#991b1b';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(16, -8);
    ctx.lineTo(8, -16);
    ctx.lineTo(8, 0);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(24, -8);
    ctx.lineTo(32, -16);
    ctx.lineTo(32, 0);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    ctx.beginPath();
    ctx.arc(20, -8, 4, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Floating question mark above ducky
    ctx.fillStyle = '#fbbf24';
    ctx.font = 'bold 24px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('? ? ?', 20, -75);

    ctx.restore();
  }

  /**
   * Renders the Sleepless & Guessing Storm Clouds.
   */
  public static renderStormCloud(
    ctx: CanvasRenderingContext2D,
    cloud: CloudObstacle,
    time: number
  ): void {
    ctx.save();
    ctx.translate(cloud.x, cloud.y);

    const bob = Math.sin(time * 3 + cloud.x * 0.05) * 6;
    ctx.translate(0, bob);

    ctx.fillStyle = '#334155';
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 4;

    ctx.beginPath();
    ctx.arc(-26, 0, 22, 0, Math.PI * 2);
    ctx.arc(0, -16, 26, 0, Math.PI * 2);
    ctx.arc(26, 0, 22, 0, Math.PI * 2);
    ctx.arc(0, 14, 22, 0, Math.PI * 2);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Sleepy cartoon eyes
    ctx.strokeStyle = '#94a3b8';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.moveTo(-16, -4);
    ctx.lineTo(-4, 0);
    ctx.moveTo(4, 0);
    ctx.lineTo(16, -4);
    ctx.stroke();

    // Label banner underneath
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = '#64748b';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(-80, 26, 160, 24, 6);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = '#f8fafc';
    ctx.font = 'bold 12px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(cloud.label, 0, 42);

    ctx.restore();
  }

  /**
   * Renders the Arcade Super Spring Ramp (Super Mola 🌀).
   */
  public static renderSpringRamp(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    isCompressed = false
  ): void {
    ctx.save();
    ctx.translate(x, y);

    const coilHeight = isCompressed ? 14 : 32;

    // 1. Ramp Base with Yellow/Black Racing Hazard Stripes
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.roundRect(-30, 0, 60, 14, 4);
    ctx.fill();
    ctx.stroke();

    // Hazard Stripes
    ctx.fillStyle = '#fbbf24';
    for (let i = -24; i < 24; i += 12) {
      ctx.beginPath();
      ctx.moveTo(i, 2);
      ctx.lineTo(i + 6, 2);
      ctx.lineTo(i + 2, 12);
      ctx.lineTo(i - 4, 12);
      ctx.closePath();
      ctx.fill();
    }

    // 2. Coiled Super Spring (Chrome / Gold)
    ctx.strokeStyle = '#e2e8f0';
    ctx.lineWidth = 5;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(-16, 0);
    ctx.lineTo(16, -coilHeight * 0.3);
    ctx.lineTo(-16, -coilHeight * 0.6);
    ctx.lineTo(16, -coilHeight * 0.85);
    ctx.lineTo(0, -coilHeight);
    ctx.stroke();

    // 3. Top Launch Pad
    ctx.fillStyle = '#ef4444';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.roundRect(-24, -coilHeight - 8, 48, 8, 3);
    ctx.fill();
    ctx.stroke();

    // Flashing Jump Arrows (▲▲▲)
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 12px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('▲ ▲ ▲', 0, -coilHeight - 14);

    ctx.restore();
  }

  /**
   * Renders the Mega Mystery Block [ ? ].
   */
  public static renderMegaMysteryBlock(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    hitCount: number,
    isSupercharged: boolean,
    time: number
  ): void {
    ctx.save();
    ctx.translate(x, y);

    const bob = Math.sin(time * 4) * 10;
    ctx.translate(0, bob);

    const shake = hitCount > 0 ? (Math.random() - 0.5) * (hitCount * 5) : 0;
    ctx.translate(shake, 0);

    const baseSize = 96;
    const size = baseSize + hitCount * 6;

    // Outer Block Frame
    ctx.fillStyle = isSupercharged ? '#ffffff' : '#7c3aed';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 6;
    ctx.beginPath();
    ctx.roundRect(-size / 2, -size / 2, size, size, 16);
    ctx.fill();
    ctx.stroke();

    // Corner Bolts
    ctx.fillStyle = '#fbbf24';
    const boltOffset = size / 2 - 14;
    ctx.beginPath();
    ctx.arc(-boltOffset, -boltOffset, 6, 0, Math.PI * 2);
    ctx.arc(boltOffset, -boltOffset, 6, 0, Math.PI * 2);
    ctx.arc(-boltOffset, boltOffset, 6, 0, Math.PI * 2);
    ctx.arc(boltOffset, boltOffset, 6, 0, Math.PI * 2);
    ctx.fill();

    // Progressive Cracks
    if (hitCount > 0) {
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 4;
      ctx.beginPath();
      ctx.moveTo(0, -size / 3);
      ctx.lineTo(10, -12);
      ctx.lineTo(-8, 10);
      if (hitCount >= 2) ctx.lineTo(14, 20);
      if (hitCount >= 3) {
        ctx.moveTo(-size / 3, 0);
        ctx.lineTo(-10, 8);
        ctx.lineTo(8, -5);
      }
      if (hitCount >= 4) {
        ctx.moveTo(size / 3, 12);
        ctx.lineTo(12, 0);
        ctx.lineTo(0, size / 3);
      }
      ctx.stroke();
    }

    // Glowing Question Mark (?)
    ctx.fillStyle = '#ffffff';
    ctx.font = `900 ${52 + hitCount * 5}px "Impact", "Arial Black", sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('?', 0, 0);

    ctx.restore();
  }

  /**
   * Renders the Grand Reveal Winner's Podium & Checkered Flags.
   */
  public static renderWinnerPodium(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    time: number
  ): void {
    ctx.save();
    ctx.translate(x, y);

    // 1. Golden Stepped Podium
    ctx.fillStyle = '#b45309';
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.roundRect(-140, 0, 280, 50, [12, 12, 0, 0]);
    ctx.fill();
    ctx.stroke();

    // Top Step
    ctx.fillStyle = '#f59e0b';
    ctx.beginPath();
    ctx.roundRect(-100, -22, 200, 24, [8, 8, 0, 0]);
    ctx.fill();
    ctx.stroke();

    // Center "1" Golden Plaque
    ctx.fillStyle = '#fbbf24';
    ctx.beginPath();
    ctx.arc(0, 22, 18, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2.5;
    ctx.stroke();

    ctx.fillStyle = '#0f172a';
    ctx.font = '900 22px "Impact", sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('1', 0, 23);

    // 2. Checkered Victory Flags on both sides
    const flagWave1 = Math.sin(time * 6) * 6;
    const flagWave2 = Math.sin(time * 6 + 1.2) * 6;

    // Left Flag Pole
    ctx.strokeStyle = '#e2e8f0';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(-130, 0);
    ctx.lineTo(-130, -90);
    ctx.stroke();

    // Left Checkered Flag
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.moveTo(-130, -90);
    ctx.quadraticCurveTo(-100, -90 + flagWave1, -70, -75);
    ctx.lineTo(-70, -45);
    ctx.quadraticCurveTo(-100, -60 + flagWave1, -130, -60);
    ctx.closePath();
    ctx.fill();
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Checkers pattern
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(-126, -86, 12, 12);
    ctx.fillRect(-102, -86 + flagWave1 * 0.5, 12, 12);
    ctx.fillRect(-114, -74, 12, 12);
    ctx.fillRect(-90, -74 + flagWave1 * 0.5, 12, 12);

    // Right Flag Pole
    ctx.strokeStyle = '#e2e8f0';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(130, 0);
    ctx.lineTo(130, -90);
    ctx.stroke();

    // Right Checkered Flag
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.moveTo(130, -90);
    ctx.quadraticCurveTo(160, -90 + flagWave2, 190, -75);
    ctx.lineTo(190, -45);
    ctx.quadraticCurveTo(160, -60 + flagWave2, 130, -60);
    ctx.closePath();
    ctx.fill();
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Checkers pattern
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(134, -86, 12, 12);
    ctx.fillRect(158, -86 + flagWave2 * 0.5, 12, 12);
    ctx.fillRect(146, -74, 12, 12);
    ctx.fillRect(170, -74 + flagWave2 * 0.5, 12, 12);

    ctx.restore();
  }

  // =========================================================================
  // 🍄 MARIO & SONIC DYNAMIC WORLD STRUCTURES
  // =========================================================================

  /**
   * Renders the iconic Mario Green Warp Pipe.
   */
  public static renderGreenPipe(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    width = 54,
    height = 68
  ): void {
    ctx.save();
    ctx.translate(x, y - height);

    const lipW = width + 12;
    const lipH = 22;
    const bodyW = width;
    const bodyH = height - lipH;

    // 1. Pipe Body
    const bodyGrad = ctx.createLinearGradient(-bodyW / 2, 0, bodyW / 2, 0);
    bodyGrad.addColorStop(0, '#15803d');
    bodyGrad.addColorStop(0.25, '#22c55e');
    bodyGrad.addColorStop(0.55, '#4ade80');
    bodyGrad.addColorStop(0.85, '#16a34a');
    bodyGrad.addColorStop(1, '#14532d');

    ctx.fillStyle = bodyGrad;
    ctx.strokeStyle = '#052e16';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.roundRect(-bodyW / 2, lipH, bodyW, bodyH, [0, 0, 4, 4]);
    ctx.fill();
    ctx.stroke();

    // 2. Pipe Lip (Top Collar)
    ctx.fillStyle = bodyGrad;
    ctx.beginPath();
    ctx.roundRect(-lipW / 2, 0, lipW, lipH, 4);
    ctx.fill();
    ctx.stroke();

    // Specular Highlight Line
    ctx.fillStyle = 'rgba(255, 255, 255, 0.45)';
    ctx.fillRect(-bodyW / 2 + 8, lipH + 2, 4, bodyH - 4);
    ctx.fillRect(-lipW / 2 + 8, 2, 5, lipH - 4);

    ctx.restore();
  }

  /**
   * Renders the floating Mario [ ? ] Question Block.
   */
  public static renderQuestionBlock(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    size = 38,
    time = 0
  ): void {
    ctx.save();
    ctx.translate(x, y);

    const bob = Math.sin(time * 3 + x * 0.05) * 4;
    ctx.translate(0, bob);

    // Golden Box Frame
    ctx.fillStyle = '#f59e0b';
    ctx.strokeStyle = '#78350f';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.roundRect(-size / 2, -size / 2, size, size, 6);
    ctx.fill();
    ctx.stroke();

    // Inner Bevel Highlight
    ctx.fillStyle = '#fbbf24';
    ctx.fillRect(-size / 2 + 3, -size / 2 + 3, size - 6, size - 6);

    // Corner Rivets
    ctx.fillStyle = '#78350f';
    const rOff = size / 2 - 5;
    ctx.beginPath();
    ctx.arc(-rOff, -rOff, 2, 0, Math.PI * 2);
    ctx.arc(rOff, -rOff, 2, 0, Math.PI * 2);
    ctx.arc(-rOff, rOff, 2, 0, Math.PI * 2);
    ctx.arc(rOff, rOff, 2, 0, Math.PI * 2);
    ctx.fill();

    // White Question Mark (?)
    ctx.fillStyle = '#ffffff';
    ctx.strokeStyle = '#78350f';
    ctx.lineWidth = 2.0;
    ctx.font = '900 24px "Impact", "Arial Black", sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.strokeText('?', 0, 1);
    ctx.fillText('?', 0, 1);

    ctx.restore();
  }

  /**
   * Renders the Mario Brown Brick Block.
   */
  public static renderBrickBlock(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    size = 38
  ): void {
    ctx.save();
    ctx.translate(x, y);

    // Main Brick Block
    ctx.fillStyle = '#b45309';
    ctx.strokeStyle = '#451a03';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.roundRect(-size / 2, -size / 2, size, size, 5);
    ctx.fill();
    ctx.stroke();

    // Mortar Seams
    ctx.strokeStyle = '#451a03';
    ctx.lineWidth = 2;
    // Horizontal center seam
    ctx.beginPath();
    ctx.moveTo(-size / 2, 0);
    ctx.lineTo(size / 2, 0);
    // Vertical staggered seams
    ctx.moveTo(0, -size / 2);
    ctx.lineTo(0, 0);
    ctx.moveTo(-size / 4, 0);
    ctx.lineTo(-size / 4, size / 2);
    ctx.moveTo(size / 4, 0);
    ctx.lineTo(size / 4, size / 2);
    ctx.stroke();

    // Brick Highlights
    ctx.fillStyle = '#d97706';
    ctx.fillRect(-size / 2 + 2, -size / 2 + 2, size / 2 - 3, size / 2 - 3);
    ctx.fillRect(2, -size / 2 + 2, size / 2 - 3, size / 2 - 3);

    ctx.restore();
  }

  /**
   * Renders the spinning Golden Sonic Ring.
   */
  public static renderSonicRing(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    time = 0
  ): void {
    ctx.save();
    ctx.translate(x, y);

    const spin = Math.cos(time * 7);
    ctx.scale(Math.abs(spin) * 0.85 + 0.15, 1);

    const outerR = 14;
    const innerR = 7;

    // Gold Outer Ring
    ctx.fillStyle = '#fbbf24';
    ctx.strokeStyle = '#d97706';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.arc(0, 0, outerR, 0, Math.PI * 2);
    ctx.arc(0, 0, innerR, 0, Math.PI * 2, true);
    ctx.fill();
    ctx.stroke();

    // Specular Highlight
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(-outerR * 0.4, -outerR * 0.4, 2.5, 0, Math.PI * 2);
    ctx.fill();

    ctx.restore();
  }

  /**
   * Renders the Sonic Ground Speed Dash Pad (>>>).
   */
  public static renderSpeedDashPad(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    width = 90,
    time = 0
  ): void {
    ctx.save();
    ctx.translate(x, y);

    // Metal Base Plate
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = '#ef4444';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.roundRect(-width / 2, -4, width, 12, 3);
    ctx.fill();
    ctx.stroke();

    // Animated Flashing Yellow/Red Chevrons (>>>)
    const arrowOffset = (time * 80) % 24;
    ctx.strokeStyle = '#facc15';
    ctx.lineWidth = 3.5;
    ctx.lineCap = 'round';

    for (let ax = -width / 2 + 12; ax <= width / 2 - 12; ax += 20) {
      const px = ax + (arrowOffset % 20) - 10;
      if (px < -width / 2 + 8 || px > width / 2 - 8) continue;
      ctx.beginPath();
      ctx.moveTo(px - 6, -1);
      ctx.lineTo(px + 4, 2);
      ctx.lineTo(px - 6, 5);
      ctx.stroke();
    }

    ctx.restore();
  }

  /**
   * Renders the iconic Sonic Green Hill 360° Corkscrew Loop.
   * Can be rendered in two passes: background arch and foreground arch!
   */
  public static renderSonicLoop(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    radius = 120,
    isForeground = false
  ): void {
    ctx.save();
    ctx.translate(x, y);

    const trackWidth = 32;

    if (!isForeground) {
      // 1. BACKGROUND ARCH OF THE LOOP (Back half of the circle)
      ctx.strokeStyle = '#78350f'; // Checkered dirt brown
      ctx.lineWidth = trackWidth;
      ctx.beginPath();
      ctx.arc(0, -radius, radius, 0.4 * Math.PI, 1.6 * Math.PI, false);
      ctx.stroke();

      // Checkered Soil Pattern on Loop Track
      ctx.strokeStyle = '#92400e';
      ctx.lineWidth = trackWidth - 6;
      ctx.stroke();

      // Outer Turf Grass Lip
      ctx.strokeStyle = '#22c55e';
      ctx.lineWidth = 6;
      ctx.beginPath();
      ctx.arc(0, -radius, radius + trackWidth / 2, 0.4 * Math.PI, 1.6 * Math.PI, false);
      ctx.stroke();
    } else {
      // 2. FOREGROUND ARCH OF THE LOOP (Front crossover curve)
      ctx.strokeStyle = '#78350f';
      ctx.lineWidth = trackWidth;
      ctx.beginPath();
      ctx.arc(0, -radius, radius, 1.4 * Math.PI, 2.6 * Math.PI, false);
      ctx.stroke();

      // Inner Turf Grass Lip
      ctx.strokeStyle = '#22c55e';
      ctx.lineWidth = 6;
      ctx.beginPath();
      ctx.arc(0, -radius, radius - trackWidth / 2, 1.4 * Math.PI, 2.6 * Math.PI, false);
      ctx.stroke();

      // Neon Guide Rails (Sonic Dash Rails)
      ctx.strokeStyle = '#facc15';
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.arc(0, -radius, radius, 1.4 * Math.PI, 2.6 * Math.PI, false);
      ctx.stroke();
    }

    ctx.restore();
  }

  /**
   * Renders a High-Tech Neon Cyberpunk Tunnel Arch.
   */
  public static renderNeonTunnelArch(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    width = 160,
    height = 200,
    time = 0
  ): void {
    ctx.save();
    ctx.translate(x, y - height);

    const glowColor = Math.sin(time * 5 + x * 0.02) > 0 ? '#00d2ff' : '#ff2a85';

    // Outer Neon Structural Arch
    ctx.strokeStyle = glowColor;
    ctx.lineWidth = 6;
    ctx.beginPath();
    ctx.moveTo(-width / 2, height);
    ctx.lineTo(-width / 2, 40);
    ctx.quadraticCurveTo(0, -10, width / 2, 40);
    ctx.lineTo(width / 2, height);
    ctx.stroke();

    // Inner Laser Grid Line
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(-width / 2 + 10, height);
    ctx.lineTo(-width / 2 + 10, 45);
    ctx.quadraticCurveTo(0, 0, width / 2 - 10, 45);
    ctx.lineTo(width / 2 - 10, height);
    ctx.stroke();

    // Top Holographic Speed Sign
    ctx.fillStyle = 'rgba(15, 23, 42, 0.85)';
    ctx.strokeStyle = glowColor;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(-45, -24, 90, 22, 6);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 11px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('SPEED >>>', 0, -13);

    ctx.restore();
  }

  /**
   * Renders Bowser / Dragon Castle Stone Battlements & Torches.
   */
  public static renderCastleBattlements(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    width = 140,
    height = 100,
    time = 0
  ): void {
    ctx.save();
    ctx.translate(x, y - height);

    // Stone Castle Wall
    ctx.fillStyle = '#1e293b';
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 3.5;
    ctx.beginPath();
    ctx.rect(-width / 2, 20, width, height - 20);
    ctx.fill();
    ctx.stroke();

    // Crenels (Teeth on top of battlements)
    const crenelW = 22;
    for (let cx = -width / 2; cx < width / 2; cx += crenelW * 2) {
      ctx.fillRect(cx, 0, crenelW, 20);
      ctx.strokeRect(cx, 0, crenelW, 20);
    }

    // Burning Wall Torches on both sides
    [-width / 2 + 15, width / 2 - 15].forEach((tx) => {
      ctx.fillStyle = '#78350f';
      ctx.fillRect(tx - 3, 35, 6, 18);

      // Flickering Flame
      const flameFlicker = Math.sin(time * 12 + tx) * 2;
      ctx.fillStyle = '#f97316';
      ctx.beginPath();
      ctx.ellipse(tx, 30 + flameFlicker, 6, 9, 0, 0, Math.PI * 2);
      ctx.fill();

      ctx.fillStyle = '#fde047';
      ctx.beginPath();
      ctx.arc(tx, 31 + flameFlicker, 3.5, 0, Math.PI * 2);
      ctx.fill();
    });

    ctx.restore();
  }

  /**
   * Renders the iconic Mario End-of-Level Flagpole & Golden Sphere.
   */
  public static renderMarioFlagpole(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    poleHeight = 180,
    time = 0
  ): void {
    ctx.save();
    ctx.translate(x, y);

    // Green/Stone Base
    ctx.fillStyle = '#15803d';
    ctx.strokeStyle = '#052e16';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.roundRect(-24, -20, 48, 20, [6, 6, 0, 0]);
    ctx.fill();
    ctx.stroke();

    // White Flagpole
    ctx.fillStyle = '#f8fafc';
    ctx.strokeStyle = '#334155';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(-3, -poleHeight, 6, poleHeight - 20, 2);
    ctx.fill();
    ctx.stroke();

    // Top Golden Sphere
    ctx.fillStyle = '#fbbf24';
    ctx.strokeStyle = '#d97706';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.arc(0, -poleHeight - 8, 10, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Waving Mario / Bowser Goal Flag
    const wave = Math.sin(time * 5) * 6;
    ctx.fillStyle = '#ef4444';
    ctx.strokeStyle = '#991b1b';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(3, -poleHeight + 10);
    ctx.quadraticCurveTo(24, -poleHeight + 10 + wave, 48, -poleHeight + 20);
    ctx.lineTo(48, -poleHeight + 48);
    ctx.quadraticCurveTo(24, -poleHeight + 38 + wave, 3, -poleHeight + 38);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Baby Footprint / Crown emblem on flag
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(24, -poleHeight + 28 + wave * 0.5, 6, 0, Math.PI * 2);
    ctx.fill();

    ctx.restore();
  }

  // ==========================================
  // STANDARDIZED VECTOR ICONS (Inside Tokens)
  // ==========================================

  private static renderHobbyIcon(ctx: CanvasRenderingContext2D, type: HobbyType): void {
    ctx.fillStyle = '#ffffff';
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 2.5;

    switch (type) {
      case 'gamepad':
        ctx.beginPath();
        ctx.roundRect(-12, -7, 24, 14, 4);
        ctx.fill();
        ctx.fillStyle = '#1e1b4b';
        ctx.fillRect(-9, -3, 5, 5);
        ctx.beginPath();
        ctx.arc(6, -2, 2, 0, Math.PI * 2);
        ctx.arc(9, 1, 2, 0, Math.PI * 2);
        ctx.fill();
        break;

      case 'dumbbell':
        ctx.beginPath();
        ctx.roundRect(-13, -7, 5, 14, 2.5);
        ctx.roundRect(8, -7, 5, 14, 2.5);
        ctx.fillRect(-8, -2, 16, 4);
        ctx.fill();
        break;

      case 'soccer':
        ctx.beginPath();
        ctx.arc(0, 0, 11, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = '#0f172a';
        ctx.beginPath();
        ctx.arc(0, 0, 4, 0, Math.PI * 2);
        ctx.fill();
        break;

      case 'music':
        ctx.beginPath();
        ctx.arc(-5, 4, 4, 0, Math.PI * 2);
        ctx.arc(5, 2, 4, 0, Math.PI * 2);
        ctx.fill();
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(-1, 4);
        ctx.lineTo(-1, -7);
        ctx.lineTo(9, -9);
        ctx.lineTo(9, 2);
        ctx.stroke();
        break;

      case 'coffee':
        ctx.beginPath();
        ctx.roundRect(-8, -5, 14, 13, 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(6, 1, 4, -Math.PI / 2, Math.PI / 2);
        ctx.stroke();
        break;

      case 'book':
        ctx.beginPath();
        ctx.moveTo(-10, -6);
        ctx.lineTo(0, -4);
        ctx.lineTo(10, -6);
        ctx.lineTo(10, 4);
        ctx.lineTo(0, 6);
        ctx.lineTo(-10, 4);
        ctx.closePath();
        ctx.fill();
        break;

      case 'camera':
        ctx.beginPath();
        ctx.roundRect(-10, -6, 20, 14, 3);
        ctx.fill();
        ctx.fillStyle = '#0f172a';
        ctx.beginPath();
        ctx.arc(0, 1, 4, 0, Math.PI * 2);
        ctx.fill();
        break;

      default:
        ctx.beginPath();
        ctx.arc(0, 0, 8, 0, Math.PI * 2);
        ctx.fill();
        break;
    }
  }
}
