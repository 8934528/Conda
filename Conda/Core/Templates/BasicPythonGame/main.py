import pygame
import random
import sys
import math

pygame.init()

# ================= SETTINGS =================
WIDTH, HEIGHT = 600, 600
GRID_SIZE = 30
ROWS = WIDTH // GRID_SIZE

screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption("Conda - Simple Edition")

clock = pygame.time.Clock()

# Colors
BLACK = (20, 20, 20)
GREEN = (0, 255, 100)
RED = (255, 60, 60)
WHITE = (240, 240, 240)
BLUE = (50, 150, 255)

# ================= GAME VARIABLES =================
snake = [[5, 5]]
dx, dy = 0, 0

food = [random.randint(0, ROWS - 1), random.randint(0, ROWS - 1)]
score = 0

core_pos = (WIDTH // 2, HEIGHT // 2)
pulse = 0


# ================= DRAW FUNCTIONS =================
def draw_grid():
    for x in range(0, WIDTH, GRID_SIZE):
        for y in range(0, HEIGHT, GRID_SIZE):
            pygame.draw.rect(screen, (40, 40, 40), (x, y, GRID_SIZE, GRID_SIZE), 1)


def draw_snake():
    for segment in snake:
        x, y = segment
        pygame.draw.rect(screen, GREEN, (x * GRID_SIZE, y * GRID_SIZE, GRID_SIZE, GRID_SIZE))


def draw_food():
    x, y = food
    pygame.draw.rect(screen, RED, (x * GRID_SIZE, y * GRID_SIZE, GRID_SIZE, GRID_SIZE))


def draw_core():
    global pulse
    pulse += 0.1
    radius = 40 + int(5 * math.sin(pulse))
    pygame.draw.circle(screen, BLUE, core_pos, radius)


def draw_score():
    font = pygame.font.SysFont("Arial", 24)
    text = font.render(f"Score: {score}", True, WHITE)
    screen.blit(text, (10, 10))


# ================= GAME LOGIC =================
def distance_to_core():
    head = snake[0]
    px = head[0] * GRID_SIZE + 15
    py = head[1] * GRID_SIZE + 15
    return math.sqrt((px - core_pos[0])**2 + (py - core_pos[1])**2)


def move_snake():
    global food, score

    head = snake[0]
    new_head = [head[0] + dx, head[1] + dy]
    snake.insert(0, new_head)

    if new_head == food:
        # Score depends on distance to core
        dist = distance_to_core()
        score += max(1, int(100 / (dist + 1)))

        food = [random.randint(0, ROWS - 1), random.randint(0, ROWS - 1)]
    else:
        snake.pop()


def check_collision():
    head = snake[0]

    # Wall
    if head[0] < 0 or head[0] >= ROWS or head[1] < 0 or head[1] >= ROWS:
        return True

    # Self
    if head in snake[1:]:
        return True

    return False


# ================= GAME LOOP =================
while True:
    clock.tick(10)

    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            pygame.quit()
            sys.exit()

        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_ESCAPE:
                pygame.quit()
                sys.exit()
            elif event.key == pygame.K_UP:
                dx, dy = 0, -1
            elif event.key == pygame.K_DOWN:
                dx, dy = 0, 1
            elif event.key == pygame.K_LEFT:
                dx, dy = -1, 0
            elif event.key == pygame.K_RIGHT:
                dx, dy = 1, 0

    move_snake()

    if check_collision():
        print("Game Over! Score:", score)
        pygame.quit()
        sys.exit()

    # Draw
    screen.fill(BLACK)
    draw_grid()
    draw_core()
    draw_snake()
    draw_food()
    draw_score()

    pygame.display.flip()
