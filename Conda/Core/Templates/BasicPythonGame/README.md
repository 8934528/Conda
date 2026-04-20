# Conda – Core Collector

A simple Python game built with **pygame**, where you control *Conda* (the snake) and collect points on a grid.

Unlike a basic snake game, scoring depends on how close you are to the **core** at the center of the screen.

---

## Game Concept

Move around the grid, collect food, and grow your snake.

The twist:

* The closer you are to the **blue core**, the more points you earn per food!

---

## Tech Stack

* Python 3
* pygame

---

## Project Structure

```
BasicPythonGame 
├── assets/
│   └── conda.png
├── .gitignore
├── main.py
├── README.md
└── requirements.txt
```

---

## How to Play

* ⬆️⬇️⬅️➡️ Use **arrow keys** to move

* Collect red blocks (food)

* Stay near the blue core for higher scores

* Avoid hitting:

  * Walls
  * Your own body

* Press **ESC** to quit

---

## Run the Game

```bash
pip install -r requirements.txt
python main.py
```

---

## Learning Focus

This project is great for beginners learning:

* Game loops
* Movement systems
* Collision detection
* Rendering with pygame
* Basic math (distance-based scoring)

---

## Future Improvements

You can extend this game by adding:

* Enemy AI
* Power-ups
* Sound effects
* Start menu
* High score system

---

## Goal

Keep it simple, learn the basics, and build up step by step.

---
