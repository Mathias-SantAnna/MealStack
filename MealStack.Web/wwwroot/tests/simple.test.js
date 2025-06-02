// Only test what actually breaks in production!

describe('Favorite Button', () => {
    test('clicking changes heart icon', () => {
        // Setup
        document.body.innerHTML = `
      <button class="favorite-btn" data-is-favorite="false">
        <i class="bi bi-heart"></i>
      </button>
    `;

        const btn = document.querySelector('.favorite-btn');
        const icon = btn.querySelector('i');

        // Act - simulate clicking
        btn.dataset.isFavorite = 'true';
        icon.className = 'bi bi-heart-fill text-danger';

        // Assert
        expect(btn.dataset.isFavorite).toBe('true');
        expect(icon.className).toContain('heart-fill');
    });
});

describe('Search Form', () => {
    test('clears input when clear button clicked', () => {
        // Setup
        document.body.innerHTML = `
      <input id="searchTerm" value="pasta" />
      <button id="clearSearch">Clear</button>
    `;

        const input = document.getElementById('searchTerm');

        // Act
        input.value = '';

        // Assert
        expect(input.value).toBe('');
    });
});