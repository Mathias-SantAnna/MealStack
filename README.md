# 🍽️ MealStack – Your Recipe Book 📖

MealStack is an open-source, dynamic recipe website designed to help home cooks, food enthusiasts, and chefs organize, save, and discover their favorite recipes with ease. Whether you're experimenting with new cuisines or managing a personal recipe catalog, MealStack makes it simple and fun!

## 🚀 Features

### 📝 Core Features:
- **Add Recipes (Create):** Save new recipes by providing a title, ingredients, instructions, and optional tags (e.g., "Vegetarian", "Dessert").
- **View Recipes (Read):** Browse through all recipes or search by name or tag.
- **Edit Recipes (Update):** Modify existing recipes to refine ingredients or instructions.
- **Delete Recipes (Delete):** Remove recipes you no longer need.

### ✨ Enhanced Features (Optional but Fun):
- 🔍 **Search Functionality:** Find recipes instantly by title, tag, or ingredient.
- 🏷️ **Tagging System (or Categories):** Organize recipes with custom tags like "Quick Meals" or "Gluten-Free."
- ⭐ **Favorites:** Mark recipes as favorites for quick access.

## 👥 Who is This For?
- 🍳 **Home Cooks:** Easily manage your personal cookbook.
- 🌍 **Food Enthusiasts:** Save and organize your latest recipe discoveries.
- 👨‍🍳 **Chefs & Professionals:** Maintain a structured recipe catalog for quick reference.

---

## 🛠️ Tech Stack & Implementation

### 🎨 Frontend:
- **Technologies:** HTML, CSS, JavaScript
- **Features:**
  - Responsive and user-friendly design.
  - Recipe list with options to **add, edit, delete, and view details**.
  - **Search functionality** for instant filtering.

### 🖥️ Backend:
- **Framework:** Flask (Python)
- **API Endpoints (RESTful CRUD Operations):**
  - `POST /recipes` → Add a new recipe.
  - `GET /recipes` → Retrieve all recipes.
  - `GET /recipes/<id>` → Get details of a specific recipe.
  - `PUT /recipes/<id>` → Update an existing recipe.
  - `DELETE /recipes/<id>` → Remove a recipe.

### 🗄️ Database:
- **SQLite (Lightweight & Easy to Use)**
- **Schema Structure:**
  ```plaintext
  id          - Unique identifier for each recipe.
  title       - Name of the recipe.
  ingredients - List of ingredients.
  instructions - Step-by-step cooking guide.
  tags        - Optional tags (e.g., "Dessert", "Vegetarian").
  ```

### 🚀 Deployment:
- **Backend Hosting:** Heroku or PythonAnywhere
- **Frontend Hosting:** Served as static files or integrated into Flask.

---

## 📜 License

This project is licensed under the **MIT License** – see the [LICENSE](./LICENSE) file for details.
