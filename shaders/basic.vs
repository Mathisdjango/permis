#version 330
layout(location = 0) in vec3 pos;

uniform mat4 proj, view, model;
uniform float time; // On récupère le temps pour animer la déformation

void main() {
    vec3 deformedPos = pos;
    
    // --- DÉFORMATION DE MAILLAGE (VERTEX SHADER) ---
    // On crée un effet d'ondulation (style gelée / jelly) basé sur le temps et la position du sommet
    deformedPos.x += sin(pos.y * 10.0 + time * 8.0) * 0.15;
    deformedPos.y += cos(pos.z * 10.0 + time * 8.0) * 0.15;
    deformedPos.z += sin(pos.x * 10.0 + time * 8.0) * 0.15;
    
    // On multiplie par les matrices avec la position déformée
    gl_Position = proj * view * model * vec4(deformedPos, 1.0);
}