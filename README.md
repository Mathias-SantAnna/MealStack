#

<h1 align="center"><strong>MealStack </strong> - your Recipe Book</h1>

<p align="justify">MealStack makes it easy to save, organize, and find your favorite recipes all in one place. Add, edit, and manage your personal cookbook with just a few clicks. Simple, fun, and built for home cooks like you!</p>

---

1. [**Features**](#Features)
    1. *Add Recipes* (Create): Users can add new recipes by providing a title, ingredients, instructions, and optional tags (e.g., "Vegetarian", "Dessert").
    2. *View Recipes* (Read): Users can view a list of all recipes and search for recipes by name or tag.
    3. *Edit Recipes* (Update): Users can edit existing recipes to make changes to ingredients or instructions.
    4. *Delete Recipes* Users can delete recipes they no longer need.

---

2. [**Enhanced Features (Optional but Fun)**](#Enhanced-features)
   1. *Search Functionality*: Users can search recipes by title, tag, or ingredient.
   2. *Tagging System*: Add tags like "Vegetarian" or "Dessert" to organize recipes.
   3. *Favorites*: Mark recipes as favorites for quick access.

---

3. [**Types of Users**](#Type-of-users)
    1. Home cooks who want to organize their favorite recipes
    2. Individuals exploring new cuisines and saving their discoveries.
    3. Chefs managing a personal recipe catalog.

---

4. [**How the System Will Be Implemented**](#how-the-system-will-be-implemented)

- **Frontend**:
  - Built with **HTML**, **CSS**, and **JavaScript** for a clean and responsive user interface.
  - Includes a simple recipe list with options to add, edit, delete, and view details of recipes.
  - Search functionality implemented using JavaScript for instant filtering.

- **Backend**:
  - Developed using **Flask** to provide RESTful API endpoints for managing recipes.
  - CRUD operations implemented:
    - `POST /recipes` for adding recipes.
    - `GET /recipes` for retrieving recipes.
    - `PUT /recipes/<id>` for updating existing recipes.
    - `DELETE /recipes/<id>` for deleting recipes.

- **Database**:
  - **SQLite** used to store recipe data, with the following schema:
    - `id`: Unique identifier for each recipe.
    - `title`: The recipe's name.
    - `ingredients`: List of ingredients.
    - `instructions`: Step-by-step cooking guide.
    - `tags`: Optional tags for categorization (e.g., "Dessert", "Vegetarian").

- **Deployment**:
  - The backend will be hosted on **Heroku** or **PythonAnywhere** for easy access.
  - The frontend will either be served as static files or integrated into the Flask app.

---
