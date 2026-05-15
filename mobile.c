#include "mobile.h"
#include "audioHelper.h"
#include <math.h>
#include <stdlib.h>
#include <assert.h>

static mobile_t * _mobiles = NULL;
static int _nb_mobiles = 0;

void mobile_init(int n) {
  _nb_mobiles = n;
  _mobiles = malloc(_nb_mobiles * sizeof *_mobiles);
  assert(_mobiles);
  
  for(int i = 0; i < _nb_mobiles; ++i) {
    float angle = (gl4dmSURand() * 3.14159f); 
    float radius = 1.5f + (gl4dmURand() * 2.5f); 
    
    _mobiles[i].p.x = cos(angle) * radius;
    _mobiles[i].p.y = gl4dmSURand() * 1.5f; 
    _mobiles[i].p.z = sin(angle) * radius;

    _mobiles[i].v.x = -sin(angle);
    _mobiles[i].v.y = gl4dmSURand() * 0.1f;
    _mobiles[i].v.z = cos(angle);

    _mobiles[i].r = 0.02f + 0.02f * gl4dmURand(); 
    
    _mobiles[i].couleur.x = 0.0f;
    _mobiles[i].couleur.y = 0.5f + 0.5f * gl4dmURand();
    _mobiles[i].couleur.z = 1.0f;
  }
}

void mobile_simu(void) {
  static double t0 = 0;
  double t = gl4dGetElapsedTime(), dt = (t - t0) / 1000.0;
  t0 = t;

  float energy = 0.0f;
  Uint8 * stream = ahGetAudioStream();
  int len = ahGetAudioStreamLength();
  
  if(stream && len > 0) {
    Sint16 * s16stream = (Sint16*)stream;
    long long sum = 0;
    for(int i = 0; i < len / 2; i++) sum += abs(s16stream[i]);
    energy = ((float)sum / (len / 2)) / 32768.0f;
    energy = energy * 3.0f; 
    if(energy > 1.0f) energy = 1.0f;
  }

  float speed = 1.0f + (energy * 6.0f); 

  // Position approximative du soleil (doit correspondre à b2b.c)
  float t_sec = t / 1000.0f;
  float sunZ = -40.0f + (t_sec * 0.45f); // Avance plus vite pour coller au timing de 110s
  float sunY = sin(t_sec * 0.1f) * 2.0f; 
  float sunX = cos(t_sec * 0.1f) * 2.0f;

  for(int i = 0; i < _nb_mobiles; ++i) {
    // Si la particule a déjà été détruite (rayon <= 0), on ignore
    if(_mobiles[i].r <= 0.0f) continue;

    float distToSun = sqrt(pow(_mobiles[i].p.x - sunX, 2) + pow(_mobiles[i].p.y - sunY, 2) + pow(_mobiles[i].p.z - sunZ, 2));

    // Si on est après 60 secondes, l'aspiration commence !
    if (t_sec > 60.0f) {
        // Vecteur directionnel vers le soleil
        float dirX = (sunX - _mobiles[i].p.x) / distToSun;
        float dirY = (sunY - _mobiles[i].p.y) / distToSun;
        float dirZ = (sunZ - _mobiles[i].p.z) / distToSun;

        // Force d'aspiration qui augmente avec le temps
        float pullForce = (t_sec - 60.0f) * 0.5f; 
        
        _mobiles[i].p.x += dirX * pullForce * dt;
        _mobiles[i].p.y += dirY * pullForce * dt;
        _mobiles[i].p.z += dirZ * pullForce * dt;

        // Désintégration au contact du soleil géant (rayon ~4.0)
        if (distToSun < 4.5f) {
            _mobiles[i].r -= 0.5f * dt; // Elle rétrécit jusqu'à disparaître
        }
    } else {
        // --- Comportement orbital normal avant 60s ---
        float dist = sqrt(_mobiles[i].p.x * _mobiles[i].p.x + _mobiles[i].p.z * _mobiles[i].p.z);
        _mobiles[i].p.x += _mobiles[i].v.x * speed * dt;
        _mobiles[i].p.y += _mobiles[i].v.y * speed * dt;
        _mobiles[i].p.z += _mobiles[i].v.z * speed * dt;

        float newDist = sqrt(_mobiles[i].p.x * _mobiles[i].p.x + _mobiles[i].p.z * _mobiles[i].p.z);
        _mobiles[i].p.x = (_mobiles[i].p.x / newDist) * dist;
        _mobiles[i].p.z = (_mobiles[i].p.z / newDist) * dist;
        
        _mobiles[i].v.x = -(_mobiles[i].p.z / dist);
        _mobiles[i].v.z = (_mobiles[i].p.x / dist);
    }

    _mobiles[i].couleur.x = energy; 
    _mobiles[i].couleur.y = 1.0f - energy; 
  }
}

void mobile_draw(GLuint pId, GLuint oId) {
  glUniform1i(glGetUniformLocation(pId, "renderMode"), 2); 
  
  for(int i = 0; i < _nb_mobiles; ++i) {
    if(_mobiles[i].r <= 0.0f) continue; // Ne pas dessiner si absorbée
    
    gl4duPushMatrix();
    gl4duTranslatef(_mobiles[i].p.x, _mobiles[i].p.y, _mobiles[i].p.z);
    gl4duScalef(_mobiles[i].r, _mobiles[i].r, _mobiles[i].r);
    gl4duSendMatrices();
    gl4duPopMatrix();
    
    glUniform4f(glGetUniformLocation(pId, "couleur"), _mobiles[i].couleur.x, _mobiles[i].couleur.y, _mobiles[i].couleur.z, 1.0f);
    gl4dgDraw(oId);
  }
}

void mobile_quit(void) {
  if(_mobiles) {
    free(_mobiles);
    _mobiles = NULL;
  }
}