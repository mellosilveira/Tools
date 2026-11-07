/**
 * 🎨 SkinSelectorOverlay: Pre-Game Character & Pet Customizer Screen
 *
 * Supports diverse outfit themes:
 * 1. 👕 Casual & Festa
 * 2. ⚡ Cyberpunk 2077 (Neon Samurai jackets, glowing cyberware, visor)
 * 3. 🐉 RPG Dragão (Dragon Slayer scale armor, pauldrons, dragon wings/horns)
 */

import { GameConfig, HairStyleMom, HairStyleDad, PetType, OutfitTheme, saveGameConfig } from '../config/gameConfig';
import { getTranslations } from '../config/i18n';

export class SkinSelectorOverlay {
  private container: HTMLElement;
  private config: GameConfig;
  private onSkinChanged: (newConfig: GameConfig) => void;
  private onStartGame: () => void;
  private isOpen: boolean = false;

  constructor(
    initialConfig: GameConfig,
    onSkinChanged: (newConfig: GameConfig) => void,
    onStartGame: () => void
  ) {
    this.config = initialConfig;
    this.onSkinChanged = onSkinChanged;
    this.onStartGame = onStartGame;

    this.container = document.createElement('div');
    this.container.id = 'skin-selector-overlay-root';
    this.container.className = 'skin-selector-overlay hidden';
    document.body.appendChild(this.container);

    this.render();
  }

  public open(): void {
    this.isOpen = true;
    this.container.classList.remove('hidden');
    this.render();
  }

  public close(): void {
    this.isOpen = false;
    this.container.classList.add('hidden');
  }

  public isVisible(): boolean {
    return this.isOpen;
  }

  public setConfig(newConfig: GameConfig): void {
    this.config = newConfig;
    if (this.isOpen) this.render();
  }

  private render(): void {
    const isPt = this.config.language === 'pt';
    const t = getTranslations(this.config.language);

    // Color palettes mapped by theme
    const momPalettes: Record<OutfitTheme, Array<{ name: string; color: string }>> = {
      casual: [
        { name: 'Rosa Festa', color: '#ec4899' },
        { name: 'Roxo Lavanda', color: '#a855f7' },
        { name: 'Ciano Claro', color: '#06b6d4' },
        { name: 'Amarelo Suave', color: '#eab308' },
        { name: 'Vermelho Paixão', color: '#ef4444' }
      ],
      cyberpunk: [
        { name: 'Neon Samurai', color: '#facc15' },
        { name: 'Night City Ciano', color: '#00d2ff' },
        { name: 'Cyber Magenta', color: '#ff2a85' },
        { name: 'Netrunner Roxo', color: '#9333ea' },
        { name: 'Tech Preto', color: '#0f172a' }
      ],
      dragon_knight: [
        { name: 'Escama Rubi de Fogo', color: '#dc2626' },
        { name: 'Ouro Dracônico', color: '#d97706' },
        { name: 'Aço Valiriano', color: '#334155' },
        { name: 'Dragão Esmeralda', color: '#059669' },
        { name: 'Safira Mística', color: '#2563eb' }
      ]
    };

    const dadPalettes: Record<OutfitTheme, Array<{ name: string; color: string }>> = {
      casual: [
        { name: 'Azul Arcade', color: '#3b82f6' },
        { name: 'Verde Moderno', color: '#10b981' },
        { name: 'Preto Grafite', color: '#1e293b' },
        { name: 'Laranja Casual', color: '#f97316' },
        { name: 'Vinho Elegante', color: '#881337' }
      ],
      cyberpunk: [
        { name: 'Neon Samurai', color: '#facc15' },
        { name: 'Ciano Cyberpunk', color: '#00d2ff' },
        { name: 'Magenta Neon', color: '#ff2a85' },
        { name: 'Aço Cromado', color: '#0284c7' },
        { name: 'Sombra de Night City', color: '#020617' }
      ],
      dragon_knight: [
        { name: 'Armadura Dragão de Fogo', color: '#b91c1c' },
        { name: 'Placa de Aço e Ouro', color: '#475569' },
        { name: 'Dragão de Bronze', color: '#b45309' },
        { name: 'Manto do Dragão Negro', color: '#0f172a' },
        { name: 'Cavaleiro Esmeralda', color: '#065f46' }
      ]
    };

    const petColors = [
      { name: 'Dourado/Caramelo', primary: '#eab308', secondary: '#fef08a' },
      { name: 'Branco Neve', primary: '#f8fafc', secondary: '#e2e8f0' },
      { name: 'Preto Noite', primary: '#1e293b', secondary: '#475569' },
      { name: 'Chocolate', primary: '#78350f', secondary: '#b45309' }
    ];

    const currentMomPalette = momPalettes[this.config.parents.mom.outfitTheme] || momPalettes.casual;
    const currentDadPalette = dadPalettes[this.config.parents.dad.outfitTheme] || dadPalettes.casual;

    this.container.innerHTML = `
      <div class="skin-selector-card">
        <div class="skin-header">
          <h2>👑 ${t.chooseSkinsTitle}</h2>
          <p class="skin-subtitle">${isPt ? 'Escolha entre trajes Casuais, Cyberpunk 2077 e RPG de Dragões!' : 'Choose between Casual, Cyberpunk 2077, and Dragon RPG outfits!'}</p>
        </div>

        <div class="skin-columns-grid">
          <!-- 1. MOM (MAMÃE) -->
          <div class="skin-char-panel">
            <div class="char-badge mom-badge">👩 ${t.momSkin}</div>
            
            <!-- Outfit Theme -->
            <div class="skin-option-group">
              <label>${t.outfitTheme}:</label>
              <div class="theme-buttons-row">
                <button type="button" class="theme-btn ${this.config.parents.mom.outfitTheme === 'casual' ? 'active' : ''}" data-mom-theme="casual">
                  👕 ${t.themeCasual}
                </button>
                <button type="button" class="theme-btn ${this.config.parents.mom.outfitTheme === 'cyberpunk' ? 'active' : ''}" data-mom-theme="cyberpunk">
                  ⚡ ${t.themeCyberpunk}
                </button>
                <button type="button" class="theme-btn ${this.config.parents.mom.outfitTheme === 'dragon_knight' ? 'active' : ''}" data-mom-theme="dragon_knight">
                  🐉 ${t.themeDragon}
                </button>
              </div>
            </div>

            <!-- Colors -->
            <div class="skin-option-group">
              <label>${t.outfit}:</label>
              <div class="color-swatches-row">
                ${currentMomPalette.map(o => `
                  <button type="button" class="swatch-btn ${this.config.parents.mom.outfitColor === o.color ? 'active' : ''}"
                    style="background-color: ${o.color};" data-mom-outfit="${o.color}" title="${o.name}">
                  </button>
                `).join('')}
              </div>
            </div>

            <!-- Hair -->
            <div class="skin-option-group">
              <label>${t.hair}:</label>
              <select id="sel-mom-hair" class="skin-select">
                <option value="long" ${this.config.parents.mom.hairStyle === 'long' ? 'selected' : ''}>${isPt ? 'Longo Fluido' : 'Long Flowing'}</option>
                <option value="bun" ${this.config.parents.mom.hairStyle === 'bun' ? 'selected' : ''}>${isPt ? 'Coque Alto' : 'Top Bun'}</option>
                <option value="curly" ${this.config.parents.mom.hairStyle === 'curly' ? 'selected' : ''}>${isPt ? 'Cacheado' : 'Curly'}</option>
                <option value="short" ${this.config.parents.mom.hairStyle === 'short' ? 'selected' : ''}>${isPt ? 'Curto Moderno' : 'Short Modern'}</option>
              </select>
            </div>
          </div>

          <!-- 2. DAD (PAPAI) -->
          <div class="skin-char-panel">
            <div class="char-badge dad-badge">👨 ${t.dadSkin}</div>
            
            <!-- Outfit Theme -->
            <div class="skin-option-group">
              <label>${t.outfitTheme}:</label>
              <div class="theme-buttons-row">
                <button type="button" class="theme-btn ${this.config.parents.dad.outfitTheme === 'casual' ? 'active' : ''}" data-dad-theme="casual">
                  👕 ${t.themeCasual}
                </button>
                <button type="button" class="theme-btn ${this.config.parents.dad.outfitTheme === 'cyberpunk' ? 'active' : ''}" data-dad-theme="cyberpunk">
                  ⚡ ${t.themeCyberpunk}
                </button>
                <button type="button" class="theme-btn ${this.config.parents.dad.outfitTheme === 'dragon_knight' ? 'active' : ''}" data-dad-theme="dragon_knight">
                  🐉 ${t.themeDragon}
                </button>
              </div>
            </div>

            <!-- Colors -->
            <div class="skin-option-group">
              <label>${t.outfit}:</label>
              <div class="color-swatches-row">
                ${currentDadPalette.map(o => `
                  <button type="button" class="swatch-btn ${this.config.parents.dad.outfitColor === o.color ? 'active' : ''}"
                    style="background-color: ${o.color};" data-dad-outfit="${o.color}" title="${o.name}">
                  </button>
                `).join('')}
              </div>
            </div>

            <!-- Hair -->
            <div class="skin-option-group">
              <label>${t.hair}:</label>
              <select id="sel-dad-hair" class="skin-select">
                <option value="short" ${this.config.parents.dad.hairStyle === 'short' ? 'selected' : ''}>${isPt ? 'Curto Clássico' : 'Short Classic'}</option>
                <option value="fade" ${this.config.parents.dad.hairStyle === 'fade' ? 'selected' : ''}>Fade</option>
                <option value="curly" ${this.config.parents.dad.hairStyle === 'curly' ? 'selected' : ''}>${isPt ? 'Cacheado' : 'Curly'}</option>
                <option value="bald" ${this.config.parents.dad.hairStyle === 'bald' ? 'selected' : ''}>${isPt ? 'Careca' : 'Bald'}</option>
              </select>
            </div>

            <div class="skin-toggles-row">
              <label class="toggle-pill">
                <input type="checkbox" id="chk-dad-beard" ${this.config.parents.dad.hasBeard ? 'checked' : ''} />
                🧔 ${t.beard}
              </label>
              <label class="toggle-pill">
                <input type="checkbox" id="chk-dad-glasses" ${this.config.parents.dad.hasGlasses ? 'checked' : ''} />
                👓 ${t.glasses}
              </label>
            </div>
          </div>

          <!-- 3. PET -->
          <div class="skin-char-panel">
            <div class="char-badge pet-badge">🐾 ${t.petSkin}</div>
            
            <!-- Pet Outfit Theme -->
            <div class="skin-option-group">
              <label>${t.outfitTheme}:</label>
              <div class="theme-buttons-row">
                <button type="button" class="theme-btn ${this.config.pet.outfitTheme === 'casual' ? 'active' : ''}" data-pet-theme="casual">
                  👕 ${t.themeCasual}
                </button>
                <button type="button" class="theme-btn ${this.config.pet.outfitTheme === 'cyberpunk' ? 'active' : ''}" data-pet-theme="cyberpunk">
                  ⚡ ${t.themeCyberpunk}
                </button>
                <button type="button" class="theme-btn ${this.config.pet.outfitTheme === 'dragon_knight' ? 'active' : ''}" data-pet-theme="dragon_knight">
                  🐉 ${t.themeDragon}
                </button>
              </div>
            </div>

            <div class="skin-option-group">
              <label>${t.petType}:</label>
              <div class="pet-type-selector">
                <button type="button" class="pet-choice-btn ${this.config.pet.enabled && this.config.pet.type === 'dog' ? 'active' : ''}" data-pet-type="dog">
                  🐶 ${isPt ? 'Cachorro' : 'Dog'}
                </button>
                <button type="button" class="pet-choice-btn ${this.config.pet.enabled && this.config.pet.type === 'cat' ? 'active' : ''}" data-pet-type="cat">
                  🐱 ${isPt ? 'Gato' : 'Cat'}
                </button>
                <button type="button" class="pet-choice-btn ${!this.config.pet.enabled || this.config.pet.type === 'none' ? 'active' : ''}" data-pet-type="none">
                  🚫 ${isPt ? 'Nenhum' : 'None'}
                </button>
              </div>
            </div>

            <div class="skin-option-group">
              <label>${t.petColor}:</label>
              <div class="color-swatches-row">
                ${petColors.map(c => `
                  <button type="button" class="swatch-btn ${this.config.pet.primaryColor === c.primary ? 'active' : ''}"
                    style="background-color: ${c.primary};" data-pet-color-primary="${c.primary}" data-pet-color-secondary="${c.secondary}" title="${c.name}">
                  </button>
                `).join('')}
              </div>
            </div>
          </div>
        </div>

        <div class="skin-footer">
          <button type="button" class="start-run-btn" id="btn-confirm-start">
            ${t.startRun}
          </button>
        </div>
      </div>
    `;

    this.bindEvents();
  }

  private bindEvents(): void {
    // Mom theme buttons
    this.container.querySelectorAll('[data-mom-theme]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const theme = (btn as HTMLElement).dataset.momTheme as OutfitTheme;
        if (theme) {
          this.config.parents.mom.outfitTheme = theme;
          // Set default color for theme
          if (theme === 'cyberpunk') this.config.parents.mom.outfitColor = '#facc15';
          else if (theme === 'dragon_knight') this.config.parents.mom.outfitColor = '#dc2626';
          else this.config.parents.mom.outfitColor = '#ec4899';
          this.notifyChange();
          this.render();
        }
      });
    });

    // Mom outfit swatches
    this.container.querySelectorAll('[data-mom-outfit]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const color = (btn as HTMLElement).dataset.momOutfit;
        if (color) {
          this.config.parents.mom.outfitColor = color;
          this.notifyChange();
          this.render();
        }
      });
    });

    // Mom hair
    const momHairSelect = this.container.querySelector('#sel-mom-hair') as HTMLSelectElement | null;
    momHairSelect?.addEventListener('change', () => {
      this.config.parents.mom.hairStyle = momHairSelect.value as HairStyleMom;
      this.notifyChange();
    });

    // Dad theme buttons
    this.container.querySelectorAll('[data-dad-theme]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const theme = (btn as HTMLElement).dataset.dadTheme as OutfitTheme;
        if (theme) {
          this.config.parents.dad.outfitTheme = theme;
          if (theme === 'cyberpunk') this.config.parents.dad.outfitColor = '#00d2ff';
          else if (theme === 'dragon_knight') this.config.parents.dad.outfitColor = '#475569';
          else this.config.parents.dad.outfitColor = '#3b82f6';
          this.notifyChange();
          this.render();
        }
      });
    });

    // Dad outfit swatches
    this.container.querySelectorAll('[data-dad-outfit]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const color = (btn as HTMLElement).dataset.dadOutfit;
        if (color) {
          this.config.parents.dad.outfitColor = color;
          this.notifyChange();
          this.render();
        }
      });
    });

    // Dad hair
    const dadHairSelect = this.container.querySelector('#sel-dad-hair') as HTMLSelectElement | null;
    dadHairSelect?.addEventListener('change', () => {
      this.config.parents.dad.hairStyle = dadHairSelect.value as HairStyleDad;
      this.notifyChange();
    });

    // Dad beard & glasses
    const dadBeardChk = this.container.querySelector('#chk-dad-beard') as HTMLInputElement | null;
    dadBeardChk?.addEventListener('change', () => {
      this.config.parents.dad.hasBeard = dadBeardChk.checked;
      this.notifyChange();
    });

    const dadGlassesChk = this.container.querySelector('#chk-dad-glasses') as HTMLInputElement | null;
    dadGlassesChk?.addEventListener('change', () => {
      this.config.parents.dad.hasGlasses = dadGlassesChk.checked;
      this.notifyChange();
    });

    // Pet theme buttons
    this.container.querySelectorAll('[data-pet-theme]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const theme = (btn as HTMLElement).dataset.petTheme as OutfitTheme;
        if (theme) {
          this.config.pet.outfitTheme = theme;
          this.notifyChange();
          this.render();
        }
      });
    });

    // Pet type buttons
    this.container.querySelectorAll('[data-pet-type]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const pType = (btn as HTMLElement).dataset.petType as PetType;
        if (pType === 'none') {
          this.config.pet.enabled = false;
          this.config.pet.type = 'none';
        } else {
          this.config.pet.enabled = true;
          this.config.pet.type = pType;
        }
        this.notifyChange();
        this.render();
      });
    });

    // Pet color swatches
    this.container.querySelectorAll('[data-pet-color-primary]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const prim = (btn as HTMLElement).dataset.petColorPrimary;
        const sec = (btn as HTMLElement).dataset.petColorSecondary;
        if (prim && sec) {
          this.config.pet.primaryColor = prim;
          this.config.pet.secondaryColor = sec;
          this.notifyChange();
          this.render();
        }
      });
    });

    // Start Button
    this.container.querySelector('#btn-confirm-start')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.close();
      this.onStartGame();
    });
  }

  private notifyChange(): void {
    saveGameConfig(this.config);
    this.onSkinChanged(this.config);
  }
}
