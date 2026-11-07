/**
 * 🎆 ParticleSystem: High-Performance 60 FPS Particle, Firework, and Neon FX Engine
 *
 * Optimized to run silky-smooth without heavy GPU blur bottlenecks.
 */

export interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  color: string;
  size: number;
  alpha: number;
  life: number;
  maxLife: number;
  type: 'confetti' | 'spark' | 'smoke' | 'star' | 'heart' | 'ring';
  rotation?: number;
  rotationSpeed?: number;
  gravity?: number;
}

export interface FloatingText {
  x: number;
  y: number;
  text: string;
  color: string;
  size: number;
  alpha: number;
  life: number;
  maxLife: number;
  vy: number;
}

export interface Shockwave {
  x: number;
  y: number;
  radius: number;
  maxRadius: number;
  color: string;
  lineWidth: number;
  alpha: number;
}

export class ParticleSystem {
  private particles: Particle[] = [];
  private floatingTexts: FloatingText[] = [];
  private shockwaves: Shockwave[] = [];

  public reset(): void {
    this.particles = [];
    this.floatingTexts = [];
    this.shockwaves = [];
  }

  public update(dt: number): void {
    // 1. Update particles
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i];
      p.life += dt;
      if (p.life >= p.maxLife) {
        this.particles.splice(i, 1);
        continue;
      }

      p.x += p.vx * dt * 60;
      p.y += p.vy * dt * 60;
      p.vy += (p.gravity ?? 0.2) * dt * 60;
      p.alpha = Math.max(0, 1 - p.life / p.maxLife);

      if (p.rotation !== undefined && p.rotationSpeed !== undefined) {
        p.rotation += p.rotationSpeed * dt * 60;
      }
    }

    // 2. Update floating texts
    for (let i = this.floatingTexts.length - 1; i >= 0; i--) {
      const ft = this.floatingTexts[i];
      ft.life += dt;
      if (ft.life >= ft.maxLife) {
        this.floatingTexts.splice(i, 1);
        continue;
      }

      ft.y += ft.vy * dt * 60;
      ft.alpha = Math.max(0, 1 - ft.life / ft.maxLife);
    }

    // 3. Update shockwaves
    for (let i = this.shockwaves.length - 1; i >= 0; i--) {
      const sw = this.shockwaves[i];
      sw.radius += (sw.maxRadius - sw.radius) * 0.14 * (dt * 60);
      sw.alpha -= 0.035 * (dt * 60);
      if (sw.alpha <= 0 || sw.radius >= sw.maxRadius * 0.98) {
        this.shockwaves.splice(i, 1);
      }
    }
  }

  public render(ctx: CanvasRenderingContext2D): void {
    ctx.save();

    // Render Shockwaves
    for (const sw of this.shockwaves) {
      ctx.save();
      ctx.strokeStyle = sw.color;
      ctx.lineWidth = sw.lineWidth;
      ctx.globalAlpha = Math.max(0, sw.alpha);
      ctx.beginPath();
      ctx.arc(sw.x, sw.y, Math.max(0, sw.radius), 0, Math.PI * 2);
      ctx.stroke();

      // Outer glow line (fast, no shadowBlur)
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.arc(sw.x, sw.y, Math.max(0, sw.radius), 0, Math.PI * 2);
      ctx.stroke();
      ctx.restore();
    }

    // Render Particles
    for (const p of this.particles) {
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.globalAlpha = p.alpha;

      if (p.type === 'confetti') {
        if (p.rotation !== undefined) ctx.rotate(p.rotation);
        const flutterScale = Math.cos(p.rotation ?? 0);
        ctx.fillStyle = p.color;
        ctx.fillRect(-p.size / 2, (-p.size * flutterScale) / 2, p.size, p.size * 0.6);
      } else if (p.type === 'spark') {
        ctx.globalCompositeOperation = 'lighter';
        ctx.fillStyle = p.color;
        ctx.beginPath();
        ctx.arc(0, 0, p.size, 0, Math.PI * 2);
        ctx.fill();
        // Inner hot core
        ctx.fillStyle = '#ffffff';
        ctx.beginPath();
        ctx.arc(0, 0, p.size * 0.4, 0, Math.PI * 2);
        ctx.fill();
      } else if (p.type === 'star') {
        ctx.fillStyle = p.color;
        this.drawStar(ctx, 0, 0, p.size);
      } else if (p.type === 'heart') {
        ctx.fillStyle = p.color;
        this.drawHeart(ctx, 0, 0, p.size);
      } else if (p.type === 'smoke') {
        ctx.fillStyle = p.color;
        ctx.beginPath();
        ctx.arc(0, 0, p.size, 0, Math.PI * 2);
        ctx.fill();
      }

      ctx.restore();
    }

    // Render Floating Arcade Texts
    for (const ft of this.floatingTexts) {
      ctx.save();
      ctx.globalAlpha = ft.alpha;
      ctx.font = `900 ${ft.size}px "Impact", "Arial Black", sans-serif`;
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';

      // Thick black stroke for instant arcade legibility
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 5;
      ctx.strokeText(ft.text, ft.x, ft.y);

      ctx.fillStyle = ft.color;
      ctx.fillText(ft.text, ft.x, ft.y);
      ctx.restore();
    }

    ctx.restore();
  }

  public spawnFloatingText(x: number, y: number, text: string, color = '#fbbf24', size = 24): void {
    this.floatingTexts.push({
      x,
      y,
      text,
      color,
      size,
      alpha: 1.0,
      life: 0,
      maxLife: 0.85,
      vy: -2.4
    });
  }

  public spawnShockwave(x: number, y: number, color = '#00d2ff', maxRadius = 160): void {
    this.shockwaves.push({
      x,
      y,
      radius: 10,
      maxRadius,
      color,
      lineWidth: 4,
      alpha: 1.0
    });
  }

  public spawnSparkles(x: number, y: number, color = '#fbbf24', count = 6): void {
    for (let i = 0; i < count; i++) {
      const angle = (i / count) * Math.PI * 2 + (Math.random() - 0.5) * 0.4;
      const speed = 1.5 + Math.random() * 3.5;
      this.particles.push({
        x,
        y,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed - 1.0,
        color,
        size: 3 + Math.random() * 3,
        alpha: 1.0,
        life: 0,
        maxLife: 0.35 + Math.random() * 0.25,
        type: 'star',
        rotation: Math.random() * Math.PI,
        rotationSpeed: (Math.random() - 0.5) * 0.4,
        gravity: 0.1
      });
    }
  }

  public spawnSpeedSparks(x: number, y: number, color: string): void {
    for (let i = 0; i < 2; i++) {
      this.particles.push({
        x: x + (Math.random() - 0.5) * 8,
        y: y + (Math.random() - 0.5) * 6,
        vx: -2.5 - Math.random() * 3,
        vy: (Math.random() - 0.5) * 2,
        color,
        size: 3 + Math.random() * 2.5,
        alpha: 1.0,
        life: 0,
        maxLife: 0.3 + Math.random() * 0.15,
        type: 'spark',
        gravity: 0.05
      });
    }
  }

  public spawnTrollBurst(x: number, y: number): void {
    const colors = ['#fde047', '#eab308', '#ffffff', '#fb923c'];
    for (let i = 0; i < 45; i++) {
      const angle = Math.random() * Math.PI * 2;
      const speed = 2.5 + Math.random() * 7;
      this.particles.push({
        x,
        y,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed - 2,
        color: colors[Math.floor(Math.random() * colors.length)],
        size: 7 + Math.random() * 7,
        alpha: 1.0,
        life: 0,
        maxLife: 0.75 + Math.random() * 0.4,
        type: Math.random() > 0.4 ? 'star' : 'confetti',
        rotation: Math.random() * Math.PI,
        rotationSpeed: (Math.random() - 0.5) * 0.3,
        gravity: 0.16
      });
    }
    this.spawnShockwave(x, y, '#eab308', 220);
  }

  public spawnBlockHit(x: number, y: number, hitIndex: number, colors: string[]): void {
    const count = 20 + hitIndex * 10;
    for (let i = 0; i < count; i++) {
      const angle = Math.random() * Math.PI * 2;
      const speed = 3 + Math.random() * (5 + hitIndex * 2);
      this.particles.push({
        x,
        y,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed - 1,
        color: colors[Math.floor(Math.random() * colors.length)],
        size: 3.5 + Math.random() * 4,
        alpha: 1.0,
        life: 0,
        maxLife: 0.45 + Math.random() * 0.35,
        type: Math.random() > 0.5 ? 'spark' : 'star',
        gravity: 0.2
      });
    }
    this.spawnShockwave(x, y, colors[0], 120 + hitIndex * 35);
  }

  public spawnConfettiShower(width: number, colors: string[]): void {
    for (let i = 0; i < 6; i++) {
      this.particles.push({
        x: Math.random() * width,
        y: -10,
        vx: (Math.random() - 0.5) * 2.5,
        vy: 2.5 + Math.random() * 3.5,
        color: colors[Math.floor(Math.random() * colors.length)],
        size: 7 + Math.random() * 7,
        alpha: 1.0,
        life: 0,
        maxLife: 2.2 + Math.random() * 1.2,
        type: 'confetti',
        rotation: Math.random() * Math.PI,
        rotationSpeed: (Math.random() - 0.5) * 0.25,
        gravity: 0.08
      });
    }
  }

  public spawnFireworkBurst(x: number, y: number, mainColor: string, secondaryColor: string): void {
    const count = 70;
    const colors = [mainColor, secondaryColor, '#ffffff', '#fbbf24'];

    for (let i = 0; i < count; i++) {
      const angle = (i / count) * Math.PI * 2 + (Math.random() - 0.5) * 0.2;
      const speed = 3 + Math.random() * 7;
      this.particles.push({
        x,
        y,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed,
        color: colors[Math.floor(Math.random() * colors.length)],
        size: 3.5 + Math.random() * 2.5,
        alpha: 1.0,
        life: 0,
        maxLife: 0.9 + Math.random() * 0.5,
        type: 'spark',
        gravity: 0.12
      });
    }

    this.spawnShockwave(x, y, mainColor, 180);
  }

  private drawStar(ctx: CanvasRenderingContext2D, cx: number, cy: number, r: number): void {
    ctx.beginPath();
    for (let i = 0; i < 5; i++) {
      ctx.lineTo(
        cx + Math.cos(((18 + i * 72) * Math.PI) / 180) * r,
        cy - Math.sin(((18 + i * 72) * Math.PI) / 180) * r
      );
      ctx.lineTo(
        cx + Math.cos(((54 + i * 72) * Math.PI) / 180) * (r * 0.4),
        cy - Math.sin(((54 + i * 72) * Math.PI) / 180) * (r * 0.4)
      );
    }
    ctx.closePath();
    ctx.fill();
  }

  private drawHeart(ctx: CanvasRenderingContext2D, cx: number, cy: number, size: number): void {
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.bezierCurveTo(cx - size / 2, cy - size / 2, cx - size, cy + size / 3, cx, cy + size);
    ctx.bezierCurveTo(cx + size, cy + size / 3, cx + size / 2, cy - size / 2, cx, cy);
    ctx.closePath();
    ctx.fill();
  }
}
