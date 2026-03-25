# Chromatic

**Tech:** Unity 6, C#  
**Type:** Capstone Project (Active Development)  
**Team:** 5 people

---

## Overview

Chromatic is a 2D puzzle platformer about depression and recovery. The world begins completely grayscale - movement feels heavy, everything feels wrong. As players defeat bosses, they unlock colors one by one: red, green, blue. Each color doesn't just change what the player can do. It changes the world itself. Grass turns green. Fire turns red. The environment transforms alongside the player's emotional state. By the end, the goal isn't just to restore color to the world - it's to make the player *feel* that transformation happen.

The name comes from "chromatic," relating to color. It captures both the literal mechanic and the metaphor: numbness giving way to feeling.

---

## Core Design Philosophy

The central question driving every design decision: can a mechanic carry emotional weight?

Color in Chromatic isn't cosmetic. It's functional. A white object floats like a cloud. Paint it black and it drops like stone. This single rule creates puzzles, hazards, and environmental storytelling simultaneously. Every mechanic had to justify its existence on both a gameplay level and a thematic one. If it didn't serve both, it didn't ship.

This also meant cutting features that didn't fit. The original design included a direct combat system with a paint rifle. Playtesting made it clear that shooting enemies broke the tone the game was building. The team cut it entirely and shifted focus to puzzle-solving - keeping the boss fights, but redesigning them around environmental interaction rather than damage output.

---

## Boss AI Design

The boss AI system uses a shared BaseBoss architecture that both bosses inherit from, keeping movement, collision, phase management, and feedback logic in one place. Each boss adds its own decision-making logic on top.

**Red Warden (Aggressive Archetype)**  
Evaluates player distance continuously and closes gaps. Proactive by design - it comes to you. Telegraph: 0.7 second lean back with red color pulse before charging. Wall collision creates a natural recovery window for the player to punish. Phase transitions at 66% and 33% HP reduce cooldowns, increasing pressure without introducing new mechanics.

**Green Sentinel (Defensive Archetype)**  
Evaluates vine count and proximity rather than player distance. Reactive by design - it fortifies and forces you to solve before you can deal damage. Heals using nearby vines when health drops low. Phase 2 introduces armor that only breaks when the player destroys vines first, turning the boss into an environmental puzzle rather than a straight fight.

The mechanical differentiation between the two bosses comes entirely from decision-making logic, not art. Same minimal geometry, completely different player experience.

**Telegraph System**  
With a four-person team and no dedicated animator, the telegraph system had to work without detailed sprite animation. Every attack uses a color pulse and scale transform as the primary signal. Minimum telegraph duration is 500ms based on human reaction time research, scaling to 700ms for the heaviest attacks. The guiding rule: if a player gets hit and says "that was unfair," it's a telegraph problem, not a skill problem.

---

## Level Design

Celeste's design philosophy shaped how abilities are introduced across levels. Every new color follows the same loop: safe introduction with no enemies, practice with mild challenge, then combination under pressure. Players never encounter a new mechanic for the first time in a high-stakes situation.

The current red level demonstrates this directly. The left section introduces red objects in an isolated, low-hazard environment. The middle section requires repeated use to progress. The right section demands execution under platforming pressure. No text instructions anywhere - the layout does the teaching.

This decision was deliberate and tied to the theme. A world slowly coming back to life shouldn't explain itself. Players should discover it.

---

## What's Shipped vs. In Progress

**Complete:** Black and red object systems, Red Warden boss, Green Sentinel boss, checkpoint system, drain mechanic, color-based world transformation, environmental hazards

**In Progress:** Blue level and boss, color mixing mechanic, audio layering system

**Cut:** Direct combat system, ally management system (removed when combat was cut)

The color mixing mechanic is the feature I'm most excited about finishing. Red plus blue equals purple. New properties, new puzzle interactions, new emotional meaning. It's the moment the game stops being about recovery and starts being about possibility.

---

## Inspirations

**Inside Out** — Color as emotional state. The film's visual logic that different feelings have different colors directly informed how we assign meaning to each unlock. Red isn't just fire. It's intensity, urgency, the first sign of feeling something again.

**Iroduku: The World in Colors** — A character who lost the ability to see color because she lost the ability to feel. The anime made the link between color and emotional aliveness feel earned rather than metaphorical. That's what we're chasing.

**CLANNAD** — Using visible feedback (light orbs) to represent emotional growth without dialogue. Chromatic takes the same approach: the world changing color is the story. You don't need to be told things got better. You see it.
