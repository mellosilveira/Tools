/**
 * 🕹️ HudOverlay: Arcade HUD, Meter Gauges & Stage Prompts
 *
 * Fast 60 FPS rendering without heavy GPU blur bottlenecks.
 */

import { GameConfig, Gender } from '../config/gameConfig';
import { getTranslations, formatText } from '../config/i18n';

export class HudOverlay {
  private config: GameConfig;

  constructor(config: GameConfig) {
    this.config = config;
  }

  public setConfig(newConfig: GameConfig): void {
    this.config = newConfig;
  }

  /**
   * Renders the Animated Adventure Top Scrubber & Obstacle Alert Banner.
   */
  public renderAnimationScrubber(
    ctx: CanvasRenderingContext2D,
    width: number,
    progress: number, // 0.0 to 1.0
    obstacleTitle: string,
    obstacleSubtitle?: string
  ): void {
    ctx.save();

    const barW = Math.min(width * 0.7, 560);
    const barH = 34;
    const barX = width / 2 - barW / 2;
    const barY = 16;

    // 1. Glassmorphic Track Bar Container
    ctx.fillStyle = 'rgba(15, 23, 42, 0.88)';
    ctx.strokeStyle = '#38bdf8';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.roundRect(barX, barY, barW, barH, 17);
    ctx.fill();
    ctx.stroke();

    // 2. Track Background Line
    const trackPad = 32;
    const trackLeft = barX + trackPad;
    const trackRight = barX + barW - trackPad;
    const trackY = barY + barH / 2;

    ctx.strokeStyle = '#334155';
    ctx.lineWidth = 4;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(trackLeft, trackY);
    ctx.lineTo(trackRight, trackY);
    ctx.stroke();

    // 3. Active Progress Fill Line (Neon Cyan / Gold)
    const currentX = trackLeft + (trackRight - trackLeft) * Math.max(0, Math.min(1, progress));
    const grad = ctx.createLinearGradient(trackLeft, 0, currentX, 0);
    grad.addColorStop(0, '#00d2ff');
    grad.addColorStop(1, '#fbbf24');

    ctx.strokeStyle = grad;
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(trackLeft, trackY);
    ctx.lineTo(currentX, trackY);
    ctx.stroke();

    // 4. Milestone Checkpoint Nodes
    const milestones = [
      { p: 0.0, icon: '🚦' },
      { p: 0.22, icon: '🦆' },
      { p: 0.44, icon: '☁️' },
      { p: 0.66, icon: '⚡' },
      { p: 0.85, icon: '❓' },
      { p: 1.0, icon: '🏆' }
    ];

    milestones.forEach((m) => {
      const mx = trackLeft + (trackRight - trackLeft) * m.p;
      const isReached = progress >= m.p;

      // Node ring
      ctx.fillStyle = isReached ? '#fbbf24' : '#1e293b';
      ctx.strokeStyle = isReached ? '#ffffff' : '#64748b';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(mx, trackY, isReached ? 8 : 6, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Node icon label
      ctx.font = '11px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(m.icon, mx, trackY - 14);
    });

    // 5. Mini Animated Cart Indicator at Current Progress
    ctx.save();
    ctx.translate(currentX, trackY);
    // Glow
    ctx.fillStyle = 'rgba(251, 191, 36, 0.4)';
    ctx.beginPath();
    ctx.arc(0, 0, 14, 0, Math.PI * 2);
    ctx.fill();

    // Cart icon
    ctx.font = '16px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('🏎️', 0, -1);
    ctx.restore();

    // 6. Obstacle Alert Banner Pill (Underneath Scrubber)
    if (obstacleTitle) {
      const pillW = Math.min(width * 0.85, 480);
      const pillH = obstacleSubtitle ? 40 : 28;
      const pillX = width / 2 - pillW / 2;
      const pillY = barY + barH + 8;

      ctx.fillStyle = 'rgba(15, 23, 42, 0.92)';
      ctx.strokeStyle = '#fbbf24';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.roundRect(pillX, pillY, pillW, pillH, 8);
      ctx.fill();
      ctx.stroke();

      ctx.fillStyle = '#fbbf24';
      ctx.font = 'bold 13px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(obstacleTitle, width / 2, pillY + (obstacleSubtitle ? 14 : 14));

      if (obstacleSubtitle) {
        ctx.fillStyle = '#94a3b8';
        ctx.font = '11px sans-serif';
        ctx.fillText(obstacleSubtitle, width / 2, pillY + 28);
      }
    }

    ctx.restore();
  }

  /**
   * Renders the Top Arcade HUD (Boy vs Girl Tug-of-War + Love Bar).
   */
  public renderTopHud(
    ctx: CanvasRenderingContext2D,
    width: number,
    teamBoyRatio: number,
    lovePercent: number
  ): void {
    const t = getTranslations(this.config.language);
    const barWidth = Math.min(width * 0.45, 380);
    const barHeight = 22;
    const barX = width / 2 - barWidth / 2;
    const barY = 22;

    ctx.save();

    // 1. TEAM BOY VS TEAM GIRL BAR
    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = '#475569';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.roundRect(barX, barY, barWidth, barHeight, 6);
    ctx.fill();
    ctx.stroke();

    // Team Boy (Left)
    const boyFillWidth = barWidth * Math.max(0.05, Math.min(0.95, teamBoyRatio));
    ctx.fillStyle = this.config.themeColors.boyPrimary;
    ctx.beginPath();
    ctx.roundRect(barX, barY, boyFillWidth, barHeight, [6, 0, 0, 6]);
    ctx.fill();

    // Team Girl (Right)
    ctx.fillStyle = this.config.themeColors.girlPrimary;
    ctx.beginPath();
    ctx.roundRect(barX + boyFillWidth, barY, barWidth - boyFillWidth, barHeight, [0, 6, 6, 0]);
    ctx.fill();

    // Divider Needle
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(barX + boyFillWidth - 2, barY - 3, 4, barHeight + 6);

    // Labels
    ctx.font = 'bold 11px "Impact", "Arial Black", sans-serif';
    ctx.fillStyle = '#ffffff';
    ctx.textAlign = 'left';
    ctx.fillText(`⚡ ${t.teamBoy}`, barX + 6, barY + 15);

    ctx.textAlign = 'right';
    ctx.fillText(`${t.teamGirl} ⚡`, barX + barWidth - 6, barY + 15);

    // 2. LOVE & HYPE METER
    const loveBarWidth = barWidth * 0.75;
    const loveBarHeight = 14;
    const loveBarX = width / 2 - loveBarWidth / 2;
    const loveBarY = barY + barHeight + 8;

    ctx.fillStyle = '#1e1b4b';
    ctx.strokeStyle = '#a855f7';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(loveBarX, loveBarY, loveBarWidth, loveBarHeight, 5);
    ctx.fill();
    ctx.stroke();

    const fillW = Math.min(loveBarWidth, (loveBarWidth * lovePercent) / 100);
    if (fillW > 0) {
      const grad = ctx.createLinearGradient(loveBarX, 0, loveBarX + fillW, 0);
      grad.addColorStop(0, '#ec4899');
      grad.addColorStop(0.5, '#fbbf24');
      grad.addColorStop(1, '#a855f7');
      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.roundRect(loveBarX, loveBarY, fillW, loveBarHeight, 4);
      ctx.fill();
    }

    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 10px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(`♥ ${t.loveMeter}: ${Math.floor(lovePercent)}%`, width / 2, loveBarY + 11);

    ctx.restore();
  }

  /**
   * Renders the Title Screen Banner on Canvas with active theme badge.
   */
  public renderTitleScreen(ctx: CanvasRenderingContext2D, width: number, height: number, time: number): void {
    const t = getTranslations(this.config.language);

    ctx.save();
    ctx.textAlign = 'center';

    // Title Prefix
    ctx.font = 'bold 15px "Impact", "Arial Black", sans-serif';
    ctx.fillStyle = this.config.themeColors.arcadeGold;
    ctx.fillText(`★ ${t.titlePrefix} ★`, width / 2, height * 0.14);

    // Main Logo with thick crisp outline
    const pulse = Math.sin(time * 4) * 2;
    ctx.font = `900 ${42 + pulse}px "Impact", "Arial Black", sans-serif`;
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 7;
    ctx.strokeText(t.titleMain, width / 2, height * 0.22);
    ctx.fillStyle = '#ffffff';
    ctx.fillText(t.titleMain, width / 2, height * 0.22);

    // Subtitle
    ctx.font = 'bold 13px sans-serif';
    ctx.fillStyle = '#f8fafc';
    ctx.fillText(t.subtitle, width / 2, height * 0.27);

    // Player 1 & Player 2 Ready Tags
    const p1 = formatText(t.player1Ready, { name: this.config.parents.mom.name });
    const p2 = formatText(t.player2Ready, { name: this.config.parents.dad.name });

    ctx.font = 'bold 13px sans-serif';
    ctx.fillStyle = this.config.themeColors.girlPrimary;
    ctx.fillText(`[ ${p1} ]`, width / 2 - 130, height * 0.38);

    ctx.fillStyle = this.config.themeColors.boyPrimary;
    ctx.fillText(`[ ${p2} ]`, width / 2 + 130, height * 0.38);

    if (this.config.pet.enabled) {
      ctx.fillStyle = '#fbbf24';
      ctx.fillText(`[ 🐾 ${this.config.pet.name} ]`, width / 2, height * 0.42);
    }

    // Active Outfit Theme Badge
    const themeName =
      this.config.parents.dad.outfitTheme === 'cyberpunk'
        ? '⚡ ESTILO: CYBERPUNK 2077'
        : this.config.parents.dad.outfitTheme === 'dragon_knight'
        ? '🐉 ESTILO: RPG DE DRAGÕES'
        : '👕 ESTILO: FESTA CASUAL';

    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = this.config.themeColors.arcadeGold;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(width / 2 - 140, height * 0.46, 280, 26, 6);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = this.config.themeColors.arcadeGold;
    ctx.font = 'bold 11px sans-serif';
    ctx.fillText(themeName, width / 2, height * 0.46 + 17);

    ctx.restore();
  }

  /**
   * Renders the Grand Finale Reveal Banner.
   */
  public renderRevealBanner(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    gender: Gender,
    time: number
  ): void {
    const t = getTranslations(this.config.language);
    const mainBannerText = gender === 'boy' ? t.boyBanner : t.girlBanner;
    const bannerColor = gender === 'boy' ? this.config.themeColors.boyPrimary : this.config.themeColors.girlPrimary;

    ctx.save();
    ctx.textAlign = 'center';

    // Sunburst background
    ctx.save();
    ctx.translate(width / 2, height * 0.28);
    const rays = 20;
    ctx.fillStyle = gender === 'boy' ? 'rgba(0, 210, 255, 0.08)' : 'rgba(255, 42, 133, 0.08)';
    for (let i = 0; i < rays; i++) {
      ctx.beginPath();
      ctx.moveTo(0, 0);
      const a1 = (i / rays) * Math.PI * 2 + time * 0.4;
      const a2 = ((i + 0.5) / rays) * Math.PI * 2 + time * 0.4;
      ctx.arc(0, 0, Math.max(width, height), a1, a2);
      ctx.closePath();
      ctx.fill();
    }
    ctx.restore();

    // Giant Banner Frame
    const cardW = Math.min(width * 0.9, 740);
    const cardH = 92;
    const cardX = width / 2 - cardW / 2;
    const cardY = height * 0.20;

    ctx.fillStyle = '#0f172a';
    ctx.strokeStyle = bannerColor;
    ctx.lineWidth = 5;
    ctx.beginPath();
    ctx.roundRect(cardX, cardY, cardW, cardH, 16);
    ctx.fill();
    ctx.stroke();

    // Inner text
    const pulse = Math.sin(time * 5) * 2;
    ctx.font = `900 ${32 + pulse}px "Impact", "Arial Black", sans-serif`;
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 6;
    ctx.strokeText(mainBannerText, width / 2, cardY + 56);
    ctx.fillStyle = '#ffffff';
    ctx.fillText(mainBannerText, width / 2, cardY + 56);

    // Subtitle
    ctx.font = 'bold 15px sans-serif';
    ctx.fillStyle = '#fbbf24';
    ctx.fillText(t.revealSubtitle, width / 2, cardY + cardH + 24);

    // Stats Card
    ctx.fillStyle = 'rgba(15, 23, 42, 0.85)';
    ctx.strokeStyle = '#334155';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(width / 2 - 200, cardY + cardH + 38, 400, 48, 10);
    ctx.fill();
    ctx.stroke();

    ctx.font = 'bold 12px sans-serif';
    ctx.fillStyle = '#ffffff';
    ctx.fillText(`★ ${t.statsLove} ★`, width / 2, cardY + cardH + 58);
    ctx.fillText(t.statsSquad, width / 2, cardY + cardH + 76);

    ctx.restore();
  }
}
