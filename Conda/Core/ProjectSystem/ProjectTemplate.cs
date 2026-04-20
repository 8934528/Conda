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
.env");

            // Create main.py
            File.WriteAllText(Path.Combine(projectPath, "main.py"),
@"print(""Welcome to your Conda Game Project!"")

import pygame
import sys

pygame.init()

SCREEN_WIDTH = 800
SCREEN_HEIGHT = 600
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
pygame.display.set_caption('Conda Game')

# Colors
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)

# Game loop
clock = pygame.time.Clock()
running = True

while running:
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False
        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_ESCAPE:
                running = False

    screen.fill(BLACK)
    
    # Draw here
    pygame.draw.circle(screen, WHITE, (SCREEN_WIDTH//2, SCREEN_HEIGHT//2), 50)
    
    pygame.display.flip()
    clock.tick(60)

pygame.quit()
sys.exit()");

            // Create README.md
            File.WriteAllText(Path.Combine(projectPath, "README.md"),
@"# New Conda Game Project

Built with Conda Engine.");

            // Create requirements.txt
            File.WriteAllText(Path.Combine(projectPath, "requirements.txt"),
@"pygame");
        }
    }
}
