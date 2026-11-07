/**
 * 🌄 ParallaxBackground: Dynamic Multi-Zone Mario & Sonic World Engine
 *
 * Professional, lush, multi-layered Full HD background with organic platformer depth:
 * - Layer 1: Atmospheric Sky, Radiant Sun with God Rays, Retro Sunset, Cyber Nebula, or Volcanic Embers
 * - Layer 2: Distant Horizon (Rolling Mario mountains with soft eyes, sunset islands, cyber skyline, castle spires)
 * - Layer 3: Midground Scenery (Lush tropical palm trees with swaying fronds, rolling green hills, bushes & flowers)
 * - Layer 4: Roadside Fences, Curbing & Wildflowers
 * - Layer 5: Dynamic Platformer Ground & Road Surface (Scalloped turf grass cap, organic geological strata, curved racing highway)
 */

import { getTrackPoint } from './TrackElevation';

interface Cloud {
  x: number;
  y: number;
  scale: number;
  speed: number;
  hasFace: boolean;
}

interface Star {
  x: number;
  y: number;
  size: number;
  alpha: number;
  twinkleSpeed: number;
  color: string;
}

interface PalmTree {
  x: number;
  height: number;
  curve: number;
}

interface Flower {
  x: number;
  color: string;
  size: number;
}

export class ParallaxBackground {
  private clouds: Cloud[] = [];
  private stars: Star[] = [];
  private palmTrees: PalmTree[] = [];
  private roadsideFlowers: Flower[] = [];

  constructor() {
    // 1. Starfield (multi-color twinkling stars)
    const starColors = ['#ffffff', '#bae6fd', '#fef08a', '#f472b6', '#a78bfa'];
    for (let i = 0; i < 110; i++) {
      this.stars.push({
        x: Math.random() * 2600,
        y: Math.random() * 550,
        size: 1.2 + Math.random() * 2.6,
        alpha: 0.35 + Math.random() * 0.65,
        twinkleSpeed: 1.5 + Math.random() * 3.0,
        color: starColors[i % starColors.length]
      });
    }

    // 2. High-Quality Multi-Layer Puffy Mario Clouds
    for (let i = 0; i < 22; i++) {
      this.clouds.push({
        x: i * 220 + Math.random() * 90,
        y: 50 + (i % 4) * 45 + Math.random() * 30,
        scale: 0.75 + Math.random() * 0.55,
        speed: 10 + (i % 3) * 6,
        hasFace: i % 2 === 0
      });
    }

    // 3. Detailed Tropical Palm Trees
    for (let i = 0; i < 20; i++) {
      this.palmTrees.push({
        x: i * 260 + Math.random() * 80,
        height: 85 + Math.random() * 35,
        curve: (Math.random() - 0.5) * 16
      });
    }

    // 4. Roadside Flowers
    const flowerColors = ['#fbbf24', '#f43f5e', '#38bdf8', '#ffffff', '#a855f7'];
    for (let i = 0; i < 60; i++) {
      this.roadsideFlowers.push({
        x: i * 75 + Math.random() * 30,
        color: flowerColors[i % flowerColors.length],
        size: 5 + Math.random() * 3
      });
    }
  }

  public render(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    cameraX: number,
    time: number
  ): void {
    const groundY = height - 110;
    const zoneInfo = this.getZoneInfo(cameraX);

    // -----------------------------------------------------------------
    // LAYER 1: ATMOSPHERIC SKY, CELESTIAL BODIES & VOLUMETRIC LIGHT
    // -----------------------------------------------------------------
    this.renderDynamicSky(ctx, width, height, groundY, zoneInfo, time);

    // -----------------------------------------------------------------
    // LAYER 2: DISTANT HORIZON (MOUNTAINS, SUNSET CLIFFS, SKYLINE)
    // -----------------------------------------------------------------
    this.renderDistantHorizon(ctx, width, groundY, cameraX, zoneInfo, time);

    // -----------------------------------------------------------------
    // LAYER 3: MIDGROUND SCENERY (ROLLING HILLS, PALM TREES, BUSHES)
    // -----------------------------------------------------------------
    this.renderMidgroundScenery(ctx, width, groundY, cameraX, zoneInfo, time);

    // -----------------------------------------------------------------
    // LAYER 4: ROADSIDE CURBS, FENCES & FLOWERS
    // -----------------------------------------------------------------
    this.renderRoadsideDetails(ctx, width, groundY, cameraX, zoneInfo, time);

    // -----------------------------------------------------------------
    // LAYER 5: PLAYFIELD GROUND & ORGANIC UNDULATING ROAD
    // -----------------------------------------------------------------
    this.renderDynamicTerrain(ctx, width, height, groundY, cameraX, zoneInfo, time);
  }

  // =========================================================================
  // 🧭 ZONE INFORMATION ENGINE
  // =========================================================================

  private getZoneInfo(cameraX: number): {
    index: number;
    name: 'green_hill' | 'sonic_sunset' | 'cyber_neon' | 'castle' | 'victory';
    blend: number;
  } {
    if (cameraX < 4500) {
      return { index: 1, name: 'green_hill', blend: cameraX / 4500 };
    }
    if (cameraX < 9000) {
      return { index: 2, name: 'sonic_sunset', blend: (cameraX - 4500) / 4500 };
    }
    if (cameraX < 13500) {
      return { index: 3, name: 'cyber_neon', blend: (cameraX - 9000) / 4500 };
    }
    if (cameraX < 17500) {
      return { index: 4, name: 'castle', blend: (cameraX - 13500) / 4000 };
    }
    return { index: 5, name: 'victory', blend: 1.0 };
  }

  // =========================================================================
  // 🌤️ LAYER 1: DYNAMIC SKY & ATMOSPHERE
  // =========================================================================

  private renderDynamicSky(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    groundY: number,
    zoneInfo: { name: string },
    time: number
  ): void {
    ctx.save();
    const skyGrad = ctx.createLinearGradient(0, 0, 0, groundY + 40);

    if (zoneInfo.name === 'green_hill') {
      // Cheerful Super Mario Azure Sky with Warm Golden Horizon Haze
      skyGrad.addColorStop(0, '#0284c7'); // Deep Sky Blue
      skyGrad.addColorStop(0.35, '#38bdf8'); // Azure
      skyGrad.addColorStop(0.75, '#bae6fd'); // Pale Blue
      skyGrad.addColorStop(1, '#fef08a'); // Warm Golden Horizon
      ctx.fillStyle = skyGrad;
      ctx.fillRect(0, 0, width, height);

      // Radiant Sun with Light Bloom & God Rays
      this.renderSunWithGodRays(ctx, width * 0.78, groundY * 0.28, time);

      // Multi-layer Puffy Mario Clouds
      this.renderMarioClouds(ctx, width, time, false);
    } else if (zoneInfo.name === 'sonic_sunset') {
      // Sonic 1 / Sonic Mania Radiant Sunset (Twilight Purple -> Ruby Coral -> Radiant Gold)
      skyGrad.addColorStop(0, '#2e1065'); // Deep Twilight Violet
      skyGrad.addColorStop(0.35, '#831843'); // Deep Ruby
      skyGrad.addColorStop(0.65, '#e11d48'); // Coral Red
      skyGrad.addColorStop(0.85, '#f59e0b'); // Radiant Amber
      skyGrad.addColorStop(1, '#fde68a'); // Horizon Gold
      ctx.fillStyle = skyGrad;
      ctx.fillRect(0, 0, width, height);

      // Retro Synthwave / Sonic Giant Setting Sun with Speed Stripes
      this.renderRetroSettingSun(ctx, width * 0.75, groundY - 40, time);

      // Sunset Clouds with Golden Rims
      this.renderMarioClouds(ctx, width, time, true);
    } else if (zoneInfo.name === 'cyber_neon') {
      // Stardust Cyberpunk Midnight Sky with Nebulae
      skyGrad.addColorStop(0, '#020617'); // Pitch Obsidian
      skyGrad.addColorStop(0.4, '#0f172a'); // Midnight Blue
      skyGrad.addColorStop(0.8, '#1e1b4b'); // Deep Indigo
      skyGrad.addColorStop(1, '#3b0764'); // Purple Horizon Glow
      ctx.fillStyle = skyGrad;
      ctx.fillRect(0, 0, width, height);

      // Twinkling Starfield & Nebulae
      this.renderStarfield(ctx, width, time);
      this.renderCyberMoon(ctx, width * 0.82, groundY * 0.32, time);
    } else if (zoneInfo.name === 'castle') {
      // Bowser's Volcanic Dark Citadel Sky
      skyGrad.addColorStop(0, '#090514'); // Black Smoke
      skyGrad.addColorStop(0.35, '#450a0a'); // Volcanic Dark Crimson
      skyGrad.addColorStop(0.7, '#7f1d1d'); // Burning Red
      skyGrad.addColorStop(1, '#c2410c'); // Lava Orange Horizon
      ctx.fillStyle = skyGrad;
      ctx.fillRect(0, 0, width, height);

      // Rising Volcanic Embers & Sparks
      this.renderVolcanicEmbers(ctx, width, height, time);
    } else {
      // Victory Goal Stadium Sky with Sweeping Spotlights
      skyGrad.addColorStop(0, '#030712');
      skyGrad.addColorStop(0.35, '#0f172a');
      skyGrad.addColorStop(0.75, '#1e1b4b');
      skyGrad.addColorStop(1, '#312e81');
      ctx.fillStyle = skyGrad;
      ctx.fillRect(0, 0, width, height);

      this.renderStadiumSpotlights(ctx, width, height, time);
      this.renderStarfield(ctx, width, time);
    }

    ctx.restore();
  }

  private renderSunWithGodRays(
    ctx: CanvasRenderingContext2D,
    sunX: number,
    sunY: number,
    time: number
  ): void {
    ctx.save();

    // God Rays (Translucent Light Beams)
    const rayCount = 8;
    for (let i = 0; i < rayCount; i++) {
      const angle = (i / rayCount) * Math.PI * 2 + time * 0.05;
      const rayGrad = ctx.createLinearGradient(
        sunX,
        sunY,
        sunX + Math.cos(angle) * 350,
        sunY + Math.sin(angle) * 350
      );
      rayGrad.addColorStop(0, 'rgba(254, 240, 138, 0.22)');
      rayGrad.addColorStop(1, 'rgba(254, 240, 138, 0)');

      ctx.fillStyle = rayGrad;
      ctx.beginPath();
      ctx.moveTo(sunX, sunY);
      ctx.arc(sunX, sunY, 320, angle - 0.12, angle + 0.12);
      ctx.closePath();
      ctx.fill();
    }

    // Outer Corona Glow
    const coronaGrad = ctx.createRadialGradient(sunX, sunY, 20, sunX, sunY, 95);
    coronaGrad.addColorStop(0, 'rgba(255, 255, 255, 0.95)');
    coronaGrad.addColorStop(0.35, 'rgba(254, 240, 138, 0.55)');
    coronaGrad.addColorStop(1, 'rgba(254, 240, 138, 0)');
    ctx.fillStyle = coronaGrad;
    ctx.beginPath();
    ctx.arc(sunX, sunY, 95, 0, Math.PI * 2);
    ctx.fill();

    // Solid Sun Core
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(sunX, sunY, 34, 0, Math.PI * 2);
    ctx.fill();

    ctx.restore();
  }

  private renderRetroSettingSun(
    ctx: CanvasRenderingContext2D,
    sunX: number,
    sunY: number,
    _time: number
  ): void {
    ctx.save();
    const radius = 70;

    // Glowing Halo
    const halo = ctx.createRadialGradient(sunX, sunY, 30, sunX, sunY, radius + 40);
    halo.addColorStop(0, 'rgba(251, 191, 36, 0.45)');
    halo.addColorStop(1, 'rgba(244, 63, 94, 0)');
    ctx.fillStyle = halo;
    ctx.beginPath();
    ctx.arc(sunX, sunY, radius + 40, 0, Math.PI * 2);
    ctx.fill();

    // Sun Gradient
    const sunGrad = ctx.createLinearGradient(sunX, sunY - radius, sunX, sunY + radius);
    sunGrad.addColorStop(0, '#fef08a');
    sunGrad.addColorStop(0.45, '#f59e0b');
    sunGrad.addColorStop(1, '#ea580c');

    ctx.fillStyle = sunGrad;
    ctx.beginPath();
    ctx.arc(sunX, sunY, radius, 0, Math.PI * 2);
    ctx.fill();

    // Horizontal Speed Cutout Stripes (Classic Sonic / OutRun Sunset!)
    ctx.fillStyle = '#2e1065'; // Matches sky color
    const stripeCount = 6;
    for (let i = 0; i < stripeCount; i++) {
      const sy = sunY + 12 + i * 9;
      const stripeH = 1.8 + i * 1.1;
      const stripeW = Math.sqrt(Math.max(0, radius * radius - Math.pow(sy - sunY, 2))) * 2;
      if (stripeW > 0) {
        ctx.fillRect(sunX - stripeW / 2, sy, stripeW, stripeH);
      }
    }

    ctx.restore();
  }

  private renderMarioClouds(
    ctx: CanvasRenderingContext2D,
    width: number,
    time: number,
    isSunset: boolean
  ): void {
    ctx.save();

    for (const c of this.clouds) {
      const cx = ((c.x - time * c.speed) % (width + 250) + (width + 250)) % (width + 250) - 120;
      ctx.save();
      ctx.translate(cx, c.y);
      ctx.scale(c.scale, c.scale);

      // Cloud Base Shading & Drop Shadow
      ctx.fillStyle = isSunset ? 'rgba(131, 24, 67, 0.45)' : 'rgba(186, 230, 253, 0.55)';
      ctx.beginPath();
      ctx.arc(-24, 4, 22, 0, Math.PI * 2);
      ctx.arc(0, -8, 28, 0, Math.PI * 2);
      ctx.arc(24, 4, 22, 0, Math.PI * 2);
      ctx.arc(0, 14, 20, 0, Math.PI * 2);
      ctx.closePath();
      ctx.fill();

      // Cloud Main Bright Body
      ctx.fillStyle = isSunset ? '#fed7aa' : '#ffffff';
      ctx.beginPath();
      ctx.arc(-24, 0, 22, 0, Math.PI * 2);
      ctx.arc(0, -12, 28, 0, Math.PI * 2);
      ctx.arc(24, 0, 22, 0, Math.PI * 2);
      ctx.arc(0, 10, 20, 0, Math.PI * 2);
      ctx.closePath();
      ctx.fill();

      // Cute Smiling Eyes on selected clouds (Classic Mario World!)
      if (c.hasFace) {
        ctx.fillStyle = '#0f172a';
        ctx.beginPath();
        ctx.ellipse(-7, -4, 2.5, 4.5, 0, 0, Math.PI * 2);
        ctx.ellipse(7, -4, 2.5, 4.5, 0, 0, Math.PI * 2);
        ctx.fill();

        // Eye specular highlights
        ctx.fillStyle = '#ffffff';
        ctx.beginPath();
        ctx.arc(-6, -6, 1.2, 0, Math.PI * 2);
        ctx.arc(8, -6, 1.2, 0, Math.PI * 2);
        ctx.fill();

        // Cheerful open smile
        ctx.strokeStyle = '#0f172a';
        ctx.lineWidth = 1.8;
        ctx.beginPath();
        ctx.arc(0, 3, 4.5, 0.1 * Math.PI, 0.9 * Math.PI, false);
        ctx.stroke();

        // Soft pink blushes
        ctx.fillStyle = isSunset ? 'rgba(244, 63, 94, 0.5)' : 'rgba(251, 113, 133, 0.45)';
        ctx.beginPath();
        ctx.ellipse(-14, 1, 3.5, 2.2, 0, 0, Math.PI * 2);
        ctx.ellipse(14, 1, 3.5, 2.2, 0, 0, Math.PI * 2);
        ctx.fill();
      }

      ctx.restore();
    }

    ctx.restore();
  }

  private renderStarfield(ctx: CanvasRenderingContext2D, width: number, time: number): void {
    ctx.save();
    for (const star of this.stars) {
      const sx = (star.x % width + width) % width;
      const twinkle = Math.sin(time * star.twinkleSpeed) * 0.35 + star.alpha;
      ctx.globalAlpha = Math.max(0.15, Math.min(1, twinkle));
      ctx.fillStyle = star.color;
      ctx.beginPath();
      ctx.arc(sx, star.y, star.size, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  private renderCyberMoon(
    ctx: CanvasRenderingContext2D,
    moonX: number,
    moonY: number,
    time: number
  ): void {
    ctx.save();
    const radius = 55;

    // Cyan Neon Holographic Moon Glow
    const moonGlow = ctx.createRadialGradient(moonX, moonY, 20, moonX, moonY, radius + 30);
    moonGlow.addColorStop(0, 'rgba(0, 210, 255, 0.35)');
    moonGlow.addColorStop(1, 'rgba(0, 210, 255, 0)');
    ctx.fillStyle = moonGlow;
    ctx.beginPath();
    ctx.arc(moonX, moonY, radius + 30, 0, Math.PI * 2);
    ctx.fill();

    // Moon Crescent
    ctx.fillStyle = '#e0f2fe';
    ctx.beginPath();
    ctx.arc(moonX, moonY, radius, 0, Math.PI * 2);
    ctx.fill();

    // Holographic Orbiting Data Rings
    ctx.strokeStyle = '#00d2ff';
    ctx.lineWidth = 1.8;
    ctx.save();
    ctx.translate(moonX, moonY);
    ctx.rotate(time * 0.4);
    ctx.beginPath();
    ctx.ellipse(0, 0, radius + 22, 16, 0.3, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();

    ctx.restore();
  }

  private renderVolcanicEmbers(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    time: number
  ): void {
    ctx.save();
    const emberCount = 35;
    for (let i = 0; i < emberCount; i++) {
      const ex = ((i * 57 + time * 24) % width + width) % width;
      const ey = height - ((time * (30 + (i % 4) * 15) + i * 28) % height);
      const size = 1.5 + (i % 3) * 1.2;

      ctx.fillStyle = i % 2 === 0 ? '#f97316' : '#fde047';
      ctx.globalAlpha = 0.5 + Math.sin(time * 4 + i) * 0.4;
      ctx.beginPath();
      ctx.arc(ex, ey, size, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  private renderStadiumSpotlights(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    time: number
  ): void {
    ctx.save();
    const sweep1 = Math.sin(time * 1.2) * 0.45;
    const sweep2 = Math.cos(time * 1.2) * 0.45;

    // Cyan Spotlight (Left)
    ctx.save();
    ctx.translate(width * 0.18, height);
    ctx.rotate(sweep1 - 0.22);
    const grad1 = ctx.createLinearGradient(0, 0, 0, -height);
    grad1.addColorStop(0, 'rgba(0, 210, 255, 0.45)');
    grad1.addColorStop(1, 'rgba(0, 210, 255, 0)');
    ctx.fillStyle = grad1;
    ctx.beginPath();
    ctx.moveTo(-18, 0);
    ctx.lineTo(-95, -height * 1.15);
    ctx.lineTo(95, -height * 1.15);
    ctx.lineTo(18, 0);
    ctx.closePath();
    ctx.fill();
    ctx.restore();

    // Magenta Spotlight (Right)
    ctx.save();
    ctx.translate(width * 0.82, height);
    ctx.rotate(sweep2 + 0.22);
    const grad2 = ctx.createLinearGradient(0, 0, 0, -height);
    grad2.addColorStop(0, 'rgba(255, 42, 133, 0.45)');
    grad2.addColorStop(1, 'rgba(255, 42, 133, 0)');
    ctx.fillStyle = grad2;
    ctx.beginPath();
    ctx.moveTo(-18, 0);
    ctx.lineTo(-95, -height * 1.15);
    ctx.lineTo(95, -height * 1.15);
    ctx.lineTo(18, 0);
    ctx.closePath();
    ctx.fill();
    ctx.restore();

    ctx.restore();
  }

  // =========================================================================
  // 🏔️ LAYER 2: DISTANT HORIZON (PARALLAX SPEED: 0.10x)
  // =========================================================================

  private renderDistantHorizon(
    ctx: CanvasRenderingContext2D,
    width: number,
    groundY: number,
    cameraX: number,
    zoneInfo: { name: string },
    _time: number
  ): void {
    ctx.save();
    const horizonSpeed = 0.10;
    const offset = (cameraX * horizonSpeed) % (width * 2);

    if (zoneInfo.name === 'green_hill') {
      // 1. Far Soft Pastel Blue-Green Mountains
      ctx.fillStyle = '#6ee7b7';
      ctx.beginPath();
      ctx.moveTo(-100, groundY);
      for (let x = -100; x <= width + 100; x += 15) {
        const wx = x + offset;
        const my = groundY - 140 + Math.sin(wx * 0.0035) * 55 + Math.sin(wx * 0.007) * 20;
        ctx.lineTo(x, my);
      }
      ctx.lineTo(width + 100, groundY);
      ctx.closePath();
      ctx.fill();

      // 2. Nearer Vibrant Emerald Mountains with Smooth Shaded Peaks & Cute Eyes!
      ctx.fillStyle = '#10b981';
      ctx.beginPath();
      ctx.moveTo(-100, groundY);
      for (let x = -100; x <= width + 100; x += 15) {
        const wx = x + offset * 1.3;
        const my = groundY - 95 + Math.sin(wx * 0.0045) * 40;
        ctx.lineTo(x, my);
      }
      ctx.lineTo(width + 100, groundY);
      ctx.closePath();
      ctx.fill();

      // Classic Mario Rolling Hill Eyes on the mountain peaks!
      const eyeSpacing = 320;
      for (let ex = -100; ex <= width + 100; ex += eyeSpacing) {
        const wx = ex + offset * 1.3;
        const my = groundY - 95 + Math.sin(wx * 0.0045) * 40;
        if (Math.sin(wx * 0.0045) > 0.4) {
          ctx.fillStyle = '#064e3b';
          ctx.beginPath();
          ctx.ellipse(ex - 4, my + 14, 2, 4, 0, 0, Math.PI * 2);
          ctx.ellipse(ex + 4, my + 14, 2, 4, 0, 0, Math.PI * 2);
          ctx.fill();
        }
      }
    } else if (zoneInfo.name === 'sonic_sunset') {
      // Sonic Sunset Layered Ocean Cliffs & Distant Loop Silhouettes
      ctx.fillStyle = '#701a75'; // Deep Magenta
      ctx.beginPath();
      ctx.moveTo(-100, groundY);
      for (let x = -100; x <= width + 100; x += 20) {
        const wx = x + offset;
        const my = groundY - 110 + Math.sin(wx * 0.004) * 45;
        ctx.lineTo(x, my);
      }
      ctx.lineTo(width + 100, groundY);
      ctx.closePath();
      ctx.fill();

      // Shaded Ocean Horizon Water Line
      const waterGrad = ctx.createLinearGradient(0, groundY - 50, 0, groundY);
      waterGrad.addColorStop(0, 'rgba(217, 70, 239, 0.45)');
      waterGrad.addColorStop(1, 'rgba(112, 26, 117, 0.85)');
      ctx.fillStyle = waterGrad;
      ctx.fillRect(0, groundY - 50, width, 50);
    } else if (zoneInfo.name === 'cyber_neon') {
      // High-Tech Cyberpunk Skyline (Tiered Skyscrapers with Glowing Windows)
      ctx.fillStyle = '#090d16';
      const buildingWidth = 50;
      for (let x = -100; x <= width + 100; x += buildingWidth) {
        const bx = x - (offset % buildingWidth);
        const bHeight = 110 + Math.abs(Math.sin((x + offset) * 0.015)) * 90;
        ctx.fillRect(bx, groundY - bHeight, buildingWidth - 6, bHeight);

        // Glowing Windows Grid
        ctx.fillStyle = Math.sin(x * 0.05) > 0 ? '#00d2ff' : '#ff2a85';
        for (let wy = groundY - bHeight + 15; wy < groundY - 15; wy += 18) {
          ctx.fillRect(bx + 8, wy, 6, 8);
          ctx.fillRect(bx + 22, wy, 6, 8);
        }
        ctx.fillStyle = '#090d16';
      }
    } else {
      // Bowser Castle Dark Basalt Spires & Volcanic Peaks
      ctx.fillStyle = '#1c1917';
      const spireWidth = 65;
      for (let x = -100; x <= width + 100; x += spireWidth) {
        const sx = x - (offset % spireWidth);
        const sHeight = 130 + Math.abs(Math.sin((x + offset) * 0.02)) * 80;
        ctx.beginPath();
        ctx.moveTo(sx, groundY);
        ctx.lineTo(sx + spireWidth / 2, groundY - sHeight);
        ctx.lineTo(sx + spireWidth, groundY);
        ctx.closePath();
        ctx.fill();
      }
    }

    ctx.restore();
  }

  // =========================================================================
  // 🌴 LAYER 3: MIDGROUND SCENERY (PARALLAX SPEED: 0.32x)
  // =========================================================================

  private renderMidgroundScenery(
    ctx: CanvasRenderingContext2D,
    width: number,
    groundY: number,
    cameraX: number,
    zoneInfo: { name: string },
    time: number
  ): void {
    if (zoneInfo.name !== 'green_hill' && zoneInfo.name !== 'sonic_sunset') return;

    ctx.save();
    const treeSpeed = 0.32;
    const treeOffset = (cameraX * treeSpeed) % (width + 300);

    for (const tree of this.palmTrees) {
      const tx = ((tree.x - treeOffset) % (width + 300) + (width + 300)) % (width + 300) - 100;
      const ty = groundY - 14;

      // 1. Organic Curved Palm Trunk with Bark Rings
      ctx.strokeStyle = '#78350f';
      ctx.lineWidth = 10;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(tx, ty);
      ctx.quadraticCurveTo(tx + tree.curve, ty - tree.height * 0.5, tx + tree.curve * 1.5, ty - tree.height);
      ctx.stroke();

      // Bark Ring Accents
      ctx.strokeStyle = '#92400e';
      ctx.lineWidth = 3;
      for (let seg = 0.2; seg < 0.9; seg += 0.2) {
        const segY = ty - tree.height * seg;
        const segX = tx + tree.curve * seg;
        ctx.beginPath();
        ctx.moveTo(segX - 5, segY);
        ctx.lineTo(segX + 5, segY);
        ctx.stroke();
      }

      // 2. Coconuts Cluster
      const topX = tx + tree.curve * 1.5;
      const topY = ty - tree.height;
      ctx.fillStyle = '#451a03';
      ctx.beginPath();
      ctx.arc(topX - 4, topY + 4, 3.5, 0, Math.PI * 2);
      ctx.arc(topX + 4, topY + 4, 3.5, 0, Math.PI * 2);
      ctx.arc(topX, topY + 8, 3.5, 0, Math.PI * 2);
      ctx.fill();

      // 3. Multi-Lobed Lush Palm Fronds (Swaying gently in breeze)
      const windSway = Math.sin(time * 2.5 + tx * 0.05) * 0.12;
      const frondAngles = [-0.9, -0.4, 0.4, 0.9, -1.3, 1.3];

      ctx.fillStyle = '#16a34a';
      ctx.strokeStyle = '#15803d';
      ctx.lineWidth = 1.5;

      for (const fa of frondAngles) {
        ctx.save();
        ctx.translate(topX, topY);
        ctx.rotate(fa + windSway);
        ctx.beginPath();
        ctx.ellipse(22, 0, 24, 7, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
        ctx.restore();
      }

      // Lime Sunlit Highlights on upper fronds
      ctx.fillStyle = '#4ade80';
      [-0.4, 0.4].forEach((fa) => {
        ctx.save();
        ctx.translate(topX, topY);
        ctx.rotate(fa + windSway);
        ctx.beginPath();
        ctx.ellipse(22, -2, 16, 3, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
      });
    }

    ctx.restore();
  }

  // =========================================================================
  // 🌼 LAYER 4: ROADSIDE CURBS, FENCES & FLOWERS (PARALLAX SPEED: 0.65x)
  // =========================================================================

  private renderRoadsideDetails(
    ctx: CanvasRenderingContext2D,
    width: number,
    groundY: number,
    cameraX: number,
    zoneInfo: { name: string },
    _time: number
  ): void {
    if (zoneInfo.name !== 'green_hill') return;

    ctx.save();
    const roadsideSpeed = 0.65;
    const offset = (cameraX * roadsideSpeed) % (width + 200);

    // Cute Roadside Wildflowers
    for (const f of this.roadsideFlowers) {
      const fx = ((f.x - offset) % (width + 200) + (width + 200)) % (width + 200) - 80;
      const pt = getTrackPoint(cameraX + fx, groundY);
      const fy = pt.y - 4;

      // Stem
      ctx.strokeStyle = '#15803d';
      ctx.lineWidth = 1.8;
      ctx.beginPath();
      ctx.moveTo(fx, fy);
      ctx.lineTo(fx, fy - 8);
      ctx.stroke();

      // Flower Petals
      ctx.fillStyle = f.color;
      ctx.beginPath();
      ctx.arc(fx, fy - 10, f.size, 0, Math.PI * 2);
      ctx.fill();

      // Flower Center
      ctx.fillStyle = '#fef08a';
      ctx.beginPath();
      ctx.arc(fx, fy - 10, f.size * 0.45, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.restore();
  }

  // =========================================================================
  // 🏁 LAYER 5: PLAYFIELD GROUND & RACING HIGHWAY (PARALLAX SPEED: 1.0x)
  // =========================================================================

  private renderDynamicTerrain(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    groundBaseY: number,
    cameraX: number,
    zoneInfo: { name: string },
    _time: number
  ): void {
    ctx.save();

    // 1. Build Smooth Continuous Elevation Polygon across the viewport
    const step = 12; // Dense 12px steps for ultra-smooth organic curvature
    const points: Array<{ x: number; y: number; worldX: number; slope: number }> = [];

    for (let screenX = -step * 2; screenX <= width + step * 2; screenX += step) {
      const worldX = cameraX + screenX;
      const pt = getTrackPoint(worldX, groundBaseY);
      points.push({ x: screenX, y: pt.y, worldX, slope: pt.slope });
    }

    // 2. Base Ground Polygon Fill
    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
      ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.lineTo(width + step * 2, height);
    ctx.lineTo(-step * 2, height);
    ctx.closePath();

    if (zoneInfo.name === 'green_hill' || zoneInfo.name === 'sonic_sunset') {
      // Rich Warm Earth with Geological Strata Layers (Not a jarring Cartesian grid!)
      const earthGrad = ctx.createLinearGradient(0, groundBaseY - 30, 0, height);
      earthGrad.addColorStop(0, '#78350f'); // Warm Amber Earth
      earthGrad.addColorStop(0.35, '#92400e'); // Golden Clay Strata
      earthGrad.addColorStop(0.7, '#451a03'); // Deep Brown Bedrock
      earthGrad.addColorStop(1, '#1c1917');
      ctx.fillStyle = earthGrad;
      ctx.fill();

      // Soft Layered Wavy Geological Strata Lines
      ctx.save();
      ctx.clip();
      ctx.strokeStyle = 'rgba(251, 191, 36, 0.12)';
      ctx.lineWidth = 6;
      for (let s = 35; s <= 140; s += 25) {
        ctx.beginPath();
        ctx.moveTo(points[0].x, points[0].y + s);
        for (let i = 1; i < points.length; i++) {
          ctx.lineTo(points[i].x, points[i].y + s);
        }
        ctx.stroke();
      }
      ctx.restore();
    } else if (zoneInfo.name === 'cyber_neon') {
      // High-Tech Carbon Fiber Mag-Rail Foundation
      const cyberGrad = ctx.createLinearGradient(0, groundBaseY - 30, 0, height);
      cyberGrad.addColorStop(0, '#090d16');
      cyberGrad.addColorStop(0.5, '#020617');
      cyberGrad.addColorStop(1, '#000000');
      ctx.fillStyle = cyberGrad;
      ctx.fill();
    } else {
      // Dark Volcanic Fortress Stone
      const castleGrad = ctx.createLinearGradient(0, groundBaseY - 30, 0, height);
      castleGrad.addColorStop(0, '#1c1917');
      castleGrad.addColorStop(0.5, '#0c0a09');
      castleGrad.addColorStop(1, '#000000');
      ctx.fillStyle = castleGrad;
      ctx.fill();
    }

    // 3. Thick Scalloped Turf Grass Fringe (Mario & Sonic Classic!)
    if (zoneInfo.name === 'green_hill' || zoneInfo.name === 'sonic_sunset') {
      // Drop Shadow under grass overhang
      ctx.strokeStyle = 'rgba(15, 23, 42, 0.4)';
      ctx.lineWidth = 18;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y + 6);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y + 6);
      }
      ctx.stroke();

      // Deep Emerald Base Turf
      ctx.strokeStyle = '#15803d';
      ctx.lineWidth = 14;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
      }
      ctx.stroke();

      // Bright Vibrant Lime Turf Top
      ctx.strokeStyle = '#22c55e';
      ctx.lineWidth = 8;
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y - 2);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y - 2);
      }
      ctx.stroke();

      // Sunlit Grass Rim
      ctx.strokeStyle = '#86efac';
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y - 5);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y - 5);
      }
      ctx.stroke();
    } else {
      // High-Tech Twin Neon Rails (Cyan & Magenta)
      ctx.strokeStyle = '#00d2ff';
      ctx.lineWidth = 5;
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
      }
      ctx.stroke();

      ctx.strokeStyle = '#ff2a85';
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y + 7);
      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y + 7);
      }
      ctx.stroke();
    }

    // 4. Smooth Racing Highway Ribbon (Asphalt Surface on the road)
    const roadDepth = 24;
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = roadDepth;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y + roadDepth / 2 + 2);
    for (let i = 1; i < points.length; i++) {
      ctx.lineTo(points[i].x, points[i].y + roadDepth / 2 + 2);
    }
    ctx.stroke();

    // 5. Curved Dashed Centerline (Yellow / White Racing Stripe)
    const dashLength = 40;
    const dashGap = 35;
    const dashTotal = dashLength + dashGap;
    const dashShift = (cameraX * 1.0) % dashTotal;

    ctx.strokeStyle = zoneInfo.name === 'green_hill' ? '#fbbf24' : '#facc15';
    ctx.lineWidth = 3.5;
    ctx.setLineDash([dashLength, dashGap]);
    ctx.lineDashOffset = dashShift;

    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y + roadDepth / 2 + 2);
    for (let i = 1; i < points.length; i++) {
      ctx.lineTo(points[i].x, points[i].y + roadDepth / 2 + 2);
    }
    ctx.stroke();
    ctx.setLineDash([]); // Reset line dash

    // 6. Red-and-White Checkered Racing Curbs on Turning Sections (Mario Kart Style!)
    const curbStep = 24;
    const curbOffset = (cameraX * 1.0) % (curbStep * 2);
    for (let cx = -curbStep * 2; cx <= width + curbStep * 2; cx += curbStep) {
      const px = cx - curbOffset;
      const pt = getTrackPoint(cameraX + px, groundBaseY);
      const isRed = Math.floor((cameraX + px) / curbStep) % 2 === 0;

      ctx.save();
      ctx.translate(px, pt.y + roadDepth + 4);
      ctx.rotate(pt.slope);
      ctx.fillStyle = isRed ? '#ef4444' : '#ffffff';
      ctx.fillRect(-curbStep / 2, -2, curbStep, 5);
      ctx.restore();
    }

    ctx.restore();
  }
}
