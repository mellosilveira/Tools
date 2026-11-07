/**
 * ⚙️ CustomizerModal: Secret Admin Configuration Panel & Link Generator
 *
 * Provides a rich on-screen modal for testing and configuring all game attributes:
 * - Parents' names, skin tones, hairstyles, beard, glasses, and outfit colors.
 * - Pet type, name, and fur color.
 * - Hobbies.
 * - Revealed gender (with spoiler shield!).
 * - Language selection (PT / EN).
 * - Generates encoded shareable URL links for WhatsApp sharing.
 */

import { GameConfig, saveGameConfig, encodeConfigToUrl, DEFAULT_CONFIG, HairStyleMom, HairStyleDad, PetType, HobbyType } from '../config/gameConfig';

export class CustomizerModal {
  private config: GameConfig;
  private onConfigChanged: (newConfig: GameConfig) => void;
  private modalContainer: HTMLElement;
  private isOpen: boolean = false;
  private isSpoilerRevealed: boolean = false;

  constructor(initialConfig: GameConfig, onConfigChanged: (newConfig: GameConfig) => void) {
    this.config = JSON.parse(JSON.stringify(initialConfig));
    this.onConfigChanged = onConfigChanged;

    this.modalContainer = document.createElement('div');
    this.modalContainer.id = 'customizer-modal-root';
    this.modalContainer.className = 'customizer-modal-backdrop hidden';
    document.body.appendChild(this.modalContainer);

    this.render();
  }

  public open(): void {
    this.isOpen = true;
    this.modalContainer.classList.remove('hidden');
    this.render();
  }

  public close(): void {
    this.isOpen = false;
    this.modalContainer.classList.add('hidden');
  }

  public toggle(): void {
    if (this.isOpen) this.close();
    else this.open();
  }

  private render(): void {
    const isPt = this.config.language === 'pt';

    this.modalContainer.innerHTML = `
      <div class="customizer-modal-card">
        <div class="modal-header">
          <h2>⚙️ ${isPt ? 'Painel de Configuração Secreto' : 'Secret Customizer Panel'}</h2>
          <button class="modal-close-btn" id="modal-btn-close">✖</button>
        </div>

        <div class="modal-body">
          <!-- GENDER SECRET SECTION -->
          <div class="config-section secret-gender-box">
            <h3>🔒 ${isPt ? 'Gênero da Revelação (Resultado Secreto)' : 'Reveal Gender (Secret Result)'}</h3>
            <div class="gender-controls">
              <button type="button" class="btn-secondary" id="btn-toggle-spoiler">
                ${this.isSpoilerRevealed ? '🙈 ' + (isPt ? 'Esconder' : 'Hide') : '👁️ ' + (isPt ? 'Ver/Alterar Gênero' : 'Show/Edit Gender')}
              </button>
              <div id="gender-selection-wrapper" class="${this.isSpoilerRevealed ? '' : 'spoiler-blurred'}">
                <label class="radio-label">
                  <input type="radio" name="revealGender" value="boy" ${this.config.revealGender === 'boy' ? 'checked' : ''} />
                  👦 ${isPt ? 'Menino (Boy)' : 'Boy'}
                </label>
                <label class="radio-label">
                  <input type="radio" name="revealGender" value="girl" ${this.config.revealGender === 'girl' ? 'checked' : ''} />
                  👧 ${isPt ? 'Menina (Girl)' : 'Girl'}
                </label>
              </div>
            </div>
          </div>

          <!-- LANGUAGE & TIMEOUT -->
          <div class="config-grid-2">
            <div class="config-group">
              <label>${isPt ? 'Idioma da Interface' : 'Interface Language'}:</label>
              <select id="cfg-language">
                <option value="pt" ${this.config.language === 'pt' ? 'selected' : ''}>Português (PT-BR)</option>
                <option value="en" ${this.config.language === 'en' ? 'selected' : ''}>English (EN-US)</option>
              </select>
            </div>
            <div class="config-group">
              <label>${isPt ? 'Tempo de Auto-Avanço (Segundos)' : 'Auto-Assist Delay (Seconds)'}:</label>
              <input type="number" id="cfg-auto-assist" value="${this.config.autoAssistTimeoutMs / 1000}" min="3" max="30" />
            </div>
          </div>

          <!-- MOM (PLAYER 1) -->
          <div class="config-section">
            <h3>👩 ${isPt ? 'Mamãe (Player 1)' : 'Mom (Player 1)'}</h3>
            <div class="config-grid-3">
              <div class="config-group">
                <label>${isPt ? 'Nome' : 'Name'}:</label>
                <input type="text" id="cfg-mom-name" value="${this.config.parents.mom.name}" />
              </div>
              <div class="config-group">
                <label>${isPt ? 'Penteado' : 'Hairstyle'}:</label>
                <select id="cfg-mom-hairstyle">
                  <option value="long" ${this.config.parents.mom.hairStyle === 'long' ? 'selected' : ''}>${isPt ? 'Longo' : 'Long'}</option>
                  <option value="bun" ${this.config.parents.mom.hairStyle === 'bun' ? 'selected' : ''}>${isPt ? 'Coque' : 'Bun'}</option>
                  <option value="curly" ${this.config.parents.mom.hairStyle === 'curly' ? 'selected' : ''}>${isPt ? 'Cacheado' : 'Curly'}</option>
                  <option value="short" ${this.config.parents.mom.hairStyle === 'short' ? 'selected' : ''}>${isPt ? 'Curto' : 'Short'}</option>
                </select>
              </div>
              <div class="config-group">
                <label>${isPt ? 'Cor da Roupa' : 'Outfit Color'}:</label>
                <input type="color" id="cfg-mom-outfit" value="${this.config.parents.mom.outfitColor}" />
              </div>
            </div>
          </div>

          <!-- DAD (PLAYER 2) -->
          <div class="config-section">
            <h3>👨 ${isPt ? 'Papai (Player 2)' : 'Dad (Player 2)'}</h3>
            <div class="config-grid-3">
              <div class="config-group">
                <label>${isPt ? 'Nome' : 'Name'}:</label>
                <input type="text" id="cfg-dad-name" value="${this.config.parents.dad.name}" />
              </div>
              <div class="config-group">
                <label>${isPt ? 'Penteado' : 'Hairstyle'}:</label>
                <select id="cfg-dad-hairstyle">
                  <option value="short" ${this.config.parents.dad.hairStyle === 'short' ? 'selected' : ''}>${isPt ? 'Curto Padrão' : 'Short'}</option>
                  <option value="fade" ${this.config.parents.dad.hairStyle === 'fade' ? 'selected' : ''}>Fade</option>
                  <option value="curly" ${this.config.parents.dad.hairStyle === 'curly' ? 'selected' : ''}>${isPt ? 'Cacheado' : 'Curly'}</option>
                  <option value="bald" ${this.config.parents.dad.hairStyle === 'bald' ? 'selected' : ''}>${isPt ? 'Careca' : 'Bald'}</option>
                </select>
              </div>
              <div class="config-group">
                <label>${isPt ? 'Cor da Roupa' : 'Outfit Color'}:</label>
                <input type="color" id="cfg-dad-outfit" value="${this.config.parents.dad.outfitColor}" />
              </div>
            </div>
            <div class="checkbox-row">
              <label><input type="checkbox" id="cfg-dad-beard" ${this.config.parents.dad.hasBeard ? 'checked' : ''} /> 🧔 ${isPt ? 'Tem Barba' : 'Has Beard'}</label>
              <label><input type="checkbox" id="cfg-dad-glasses" ${this.config.parents.dad.hasGlasses ? 'checked' : ''} /> 👓 ${isPt ? 'Usa Óculos' : 'Has Glasses'}</label>
            </div>
          </div>

          <!-- PET -->
          <div class="config-section">
            <h3>🐾 ${isPt ? 'Pet da Família' : 'Family Pet'}</h3>
            <div class="config-grid-3">
              <div class="config-group">
                <label><input type="checkbox" id="cfg-pet-enabled" ${this.config.pet.enabled ? 'checked' : ''} /> ${isPt ? 'Incluir Pet' : 'Include Pet'}</label>
              </div>
              <div class="config-group">
                <label>${isPt ? 'Espécie' : 'Species'}:</label>
                <select id="cfg-pet-type">
                  <option value="dog" ${this.config.pet.type === 'dog' ? 'selected' : ''}>🐶 ${isPt ? 'Cachorro' : 'Dog'}</option>
                  <option value="cat" ${this.config.pet.type === 'cat' ? 'selected' : ''}>🐱 ${isPt ? 'Gato' : 'Cat'}</option>
                </select>
              </div>
              <div class="config-group">
                <label>${isPt ? 'Nome do Pet' : 'Pet Name'}:</label>
                <input type="text" id="cfg-pet-name" value="${this.config.pet.name}" />
              </div>
            </div>
          </div>

          <!-- HOBBIES -->
          <div class="config-section">
            <h3>🎮 ${isPt ? 'Hobbies e Power-Ups do Casal' : 'Couple Hobbies & Power-Ups'}</h3>
            <div class="hobbies-grid">
              ${this.renderHobbyCheckbox('gamepad', '🎮 Videogame')}
              ${this.renderHobbyCheckbox('dumbbell', '🏋️ Halter / Treino')}
              ${this.renderHobbyCheckbox('soccer', '⚽ Futebol')}
              ${this.renderHobbyCheckbox('music', '🎵 Música')}
              ${this.renderHobbyCheckbox('coffee', '☕ Café')}
              ${this.renderHobbyCheckbox('book', '📚 Livros')}
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn-secondary" id="modal-btn-reset">${isPt ? 'Restaurar Padrões' : 'Reset Defaults'}</button>
          <button type="button" class="btn-secondary" id="modal-btn-share">🔗 ${isPt ? 'Copiar Link Compartilhável' : 'Copy Shareable Link'}</button>
          <button type="button" class="btn-primary" id="modal-btn-save">💾 ${isPt ? 'Salvar & Aplicar' : 'Save & Apply'}</button>
        </div>
      </div>
    `;

    this.bindEvents();
  }

  private renderHobbyCheckbox(hobby: HobbyType, label: string): string {
    const isChecked = this.config.hobbies.includes(hobby);
    return `
      <label class="hobby-tag">
        <input type="checkbox" class="cfg-hobby-item" value="${hobby}" ${isChecked ? 'checked' : ''} />
        ${label}
      </label>
    `;
  }

  private bindEvents(): void {
    // Close button
    document.getElementById('modal-btn-close')?.addEventListener('click', () => this.close());

    // Spoiler Toggle
    document.getElementById('btn-toggle-spoiler')?.addEventListener('click', () => {
      this.isSpoilerRevealed = !this.isSpoilerRevealed;
      const el = document.getElementById('gender-selection-wrapper');
      const btn = document.getElementById('btn-toggle-spoiler');
      if (el && btn) {
        if (this.isSpoilerRevealed) {
          el.classList.remove('spoiler-blurred');
          btn.textContent = '🙈 ' + (this.config.language === 'pt' ? 'Esconder' : 'Hide');
        } else {
          el.classList.add('spoiler-blurred');
          btn.textContent = '👁️ ' + (this.config.language === 'pt' ? 'Ver/Alterar Gênero' : 'Show/Edit Gender');
        }
      }
    });

    // Reset Defaults
    document.getElementById('modal-btn-reset')?.addEventListener('click', () => {
      if (confirm(this.config.language === 'pt' ? 'Deseja restaurar as configurações originais?' : 'Reset to default settings?')) {
        this.config = JSON.parse(JSON.stringify(DEFAULT_CONFIG));
        saveGameConfig(this.config);
        this.onConfigChanged(this.config);
        this.render();
      }
    });

    // Copy Shareable Link
    document.getElementById('modal-btn-share')?.addEventListener('click', () => {
      this.readFormValues();
      const code = encodeConfigToUrl(this.config);
      const url = `${window.location.origin}${window.location.pathname}?cfg=${code}`;
      navigator.clipboard.writeText(url).then(() => {
        alert(this.config.language === 'pt'
          ? 'Link copiado para a área de transferência! Envie no WhatsApp ou abra na TV.'
          : 'Shareable link copied to clipboard! Send on WhatsApp or open on TV.');
      }).catch(() => {
        prompt('Copy this link:', url);
      });
    });

    // Save & Apply
    document.getElementById('modal-btn-save')?.addEventListener('click', () => {
      this.readFormValues();
      saveGameConfig(this.config);
      this.onConfigChanged(this.config);
      this.close();
    });
  }

  private readFormValues(): void {
    // Gender
    const genderRadio = document.querySelector('input[name="revealGender"]:checked') as HTMLInputElement | null;
    if (genderRadio) this.config.revealGender = genderRadio.value as 'boy' | 'girl';

    // Language
    const langSelect = document.getElementById('cfg-language') as HTMLSelectElement | null;
    if (langSelect) this.config.language = langSelect.value as 'pt' | 'en';

    // Auto assist
    const assistInput = document.getElementById('cfg-auto-assist') as HTMLInputElement | null;
    if (assistInput) this.config.autoAssistTimeoutMs = Math.max(3000, Number(assistInput.value) * 1000);

    // Mom
    const momName = document.getElementById('cfg-mom-name') as HTMLInputElement | null;
    const momHair = document.getElementById('cfg-mom-hairstyle') as HTMLSelectElement | null;
    const momOutfit = document.getElementById('cfg-mom-outfit') as HTMLInputElement | null;
    if (momName) this.config.parents.mom.name = momName.value.trim() || 'Mamãe';
    if (momHair) this.config.parents.mom.hairStyle = momHair.value as HairStyleMom;
    if (momOutfit) this.config.parents.mom.outfitColor = momOutfit.value;

    // Dad
    const dadName = document.getElementById('cfg-dad-name') as HTMLInputElement | null;
    const dadHair = document.getElementById('cfg-dad-hairstyle') as HTMLSelectElement | null;
    const dadOutfit = document.getElementById('cfg-dad-outfit') as HTMLInputElement | null;
    const dadBeard = document.getElementById('cfg-dad-beard') as HTMLInputElement | null;
    const dadGlasses = document.getElementById('cfg-dad-glasses') as HTMLInputElement | null;
    if (dadName) this.config.parents.dad.name = dadName.value.trim() || 'Papai';
    if (dadHair) this.config.parents.dad.hairStyle = dadHair.value as HairStyleDad;
    if (dadOutfit) this.config.parents.dad.outfitColor = dadOutfit.value;
    if (dadBeard) this.config.parents.dad.hasBeard = dadBeard.checked;
    if (dadGlasses) this.config.parents.dad.hasGlasses = dadGlasses.checked;

    // Pet
    const petEnabled = document.getElementById('cfg-pet-enabled') as HTMLInputElement | null;
    const petType = document.getElementById('cfg-pet-type') as HTMLSelectElement | null;
    const petName = document.getElementById('cfg-pet-name') as HTMLInputElement | null;
    if (petEnabled) this.config.pet.enabled = petEnabled.checked;
    if (petType) this.config.pet.type = petType.value as PetType;
    if (petName) this.config.pet.name = petName.value.trim() || 'Pet';

    // Hobbies
    const hobbyCheckboxes = document.querySelectorAll('.cfg-hobby-item:checked') as NodeListOf<HTMLInputElement>;
    const selectedHobbies: HobbyType[] = [];
    hobbyCheckboxes.forEach((cb) => selectedHobbies.push(cb.value as HobbyType));
    this.config.hobbies = selectedHobbies.length > 0 ? selectedHobbies : ['gamepad', 'dumbbell'];
  }
}
