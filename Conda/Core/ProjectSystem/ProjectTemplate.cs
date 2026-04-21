using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Conda.Core.ProjectSystem
{
    public static class ProjectTemplate
    {
        public static void ApplyPygameTemplate(string projectPath)
        {
            // Create .gitignore
            File.WriteAllText(Path.Combine(projectPath, ".gitignore"),
@"venv/
__pycache__/
*.pyc
.env
.DS_Store");

            // Create main.py
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"# Conda Game Project - Created with Conda IDE

import pygame
import sys
import os

# Initialize Pygame
pygame.init()

# Constants
SCREEN_WIDTH = 800
SCREEN_HEIGHT = 600
FPS = 60

# Colors
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
RED = (255, 0, 0)
GREEN = (0, 255, 0)
BLUE = (0, 0, 255)

# Setup screen
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
pygame.display.set_caption('Conda Game - Pygame Project')
clock = pygame.time.Clock()

# Game variables
running = True
player_pos = [SCREEN_WIDTH // 2, SCREEN_HEIGHT // 2]
player_size = 50

def handle_events():
    global running
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            return False
        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_ESCAPE:
                return False
    return True

def update():
    global player_pos
    keys = pygame.key.get_pressed()
    speed = 5
    
    if keys[pygame.K_LEFT] or keys[pygame.K_a]:
        player_pos[0] -= speed
    if keys[pygame.K_RIGHT] or keys[pygame.K_d]:
        player_pos[0] += speed
    if keys[pygame.K_UP] or keys[pygame.K_w]:
        player_pos[1] -= speed
    if keys[pygame.K_DOWN] or keys[pygame.K_s]:
        player_pos[1] += speed
    
    # Keep player on screen
    player_pos[0] = max(0, min(SCREEN_WIDTH - player_size, player_pos[0]))
    player_pos[1] = max(0, min(SCREEN_HEIGHT - player_size, player_pos[1]))

def draw():
    screen.fill(BLACK)
    
    # Draw player (a simple rectangle)
    pygame.draw.rect(screen, WHITE, (player_pos[0], player_pos[1], player_size, player_size))
    
    # Draw instructions
    font = pygame.font.Font(None, 36)
    text = font.render('Use WASD or Arrow Keys to move', True, WHITE)
    screen.blit(text, (SCREEN_WIDTH // 2 - text.get_width() // 2, 10))
    
    pygame.display.flip()

# Game loop
while running:
    running = handle_events()
    update()
    draw()
    clock.tick(FPS)

pygame.quit()
sys.exit()");

            // Create README.md
            File.WriteAllText(Path.Combine(projectPath, "README.md"),
@"# Conda Game Project

## Getting Started
1. Make sure you have Python and pygame installed
2. Run `main.py` to start the game
3. Use WASD or Arrow Keys to move the white square

## Project Structure
- `main.py` - Main game file
- `assets/` - Game assets (images, sounds, etc.)
- `scenes/` - Game scenes/levels
- `scripts/` - Additional Python scripts

## Controls
- WASD or Arrow Keys - Move player
- ESC - Exit game

## Customization
Edit `main.py` to change game behavior, add features, or create your own game!

Built with Conda IDE");

            // Create requirements.txt
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"),
@"pygame>=2.0.0");
        }
    }
}
