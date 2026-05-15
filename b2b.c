#include <GL4D/gl4duw_SDL2.h>
#include <GL4D/gl4dg.h>
#include <GL4D/gl4dh.h>
#include <math.h>

static void init(void);
static void draw(void);

static GLuint _pIdRay = 0;   
static GLuint _pIdCube = 0;  
static GLuint _quad = 0;
static GLuint _cube = 0;

void b2b(int state) {
  switch(state) {
  case GL4DH_INIT: init(); return;
  case GL4DH_FREE: return;
  case GL4DH_UPDATE_WITH_AUDIO: return;
  default: draw(); return;
  }
}

void init(void) {
  _quad = gl4dgGenQuadf(); 
  _cube = gl4dgGenCubef(); 

  _pIdRay = gl4duCreateProgram("<vs>shaders/b2b.vs", "<fs>shaders/b2b.fs", NULL);
  _pIdCube = gl4duCreateProgram("<vs>shaders/basic.vs", "<fs>shaders/basic.fs", NULL);

  gl4duGenMatrix(GL_FLOAT, "model");
  gl4duGenMatrix(GL_FLOAT, "view");
  gl4duGenMatrix(GL_FLOAT, "proj");
}

void draw(void) {
  double t = gl4dGetElapsedTime() / 1000.0;
  GLint vp[4];
  glGetIntegerv(GL_VIEWPORT, vp);

  glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
  glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    // 1. GESTION DE LA CAMÉRA (Avec vue plongeante)

     float camX, camY, camZ; 
  float tarX, tarY, tarZ; 

  if(t < 3.0f) {
      camX = 0.0f; camY = 6.0f; camZ = 7.0f; 
      tarX = 0.0f; tarY = 6.0f; tarZ = 0.0f; 
  } else if (t < 8.0f) {
      float progress = (t - 3.0f) / 5.0f;
      float smooth = progress * progress * progress * (progress * (6.0f * progress - 15.0f) + 10.0f);
      
      camX = 0.0f; 
      camY = 6.0f - (5.65f * smooth); // Finit à 0.35 (Un peu plus haut à l'arrière)
      camZ = 7.0f - (6.5f * smooth);  // Finit à 0.5
      
      tarX = 0.0f; 
      tarY = 6.0f - (6.1f * smooth);  // Finit à -0.1 (Regard PLONGEANT vers le frein)
      tarZ = 0.0f - (1.5f * smooth);  // Finit à -1.5
  } else {
      // Installé à l'arrière, on regarde entre les deux sièges
      camX = 0.0f; camY = 0.35f; camZ = 0.5f;
      tarX = 0.0f; tarY = -0.1f; tarZ = -1.5f;
  }

  glDisable(GL_DEPTH_TEST); 
  glUseProgram(_pIdRay);
  glUniform2f(glGetUniformLocation(_pIdRay, "resolution"), (float)vp[2], (float)vp[3]);
  glUniform1f(glGetUniformLocation(_pIdRay, "time"), t);
  glUniform3f(glGetUniformLocation(_pIdRay, "ro"), camX, camY, camZ);
  glUniform3f(glGetUniformLocation(_pIdRay, "ta"), tarX, tarY, tarZ);
  gl4dgDraw(_quad); 

  glEnable(GL_DEPTH_TEST);
  glClear(GL_DEPTH_BUFFER_BIT); 
  
  gl4duBindMatrix("proj");
  gl4duLoadIdentityf();
  float aspect = (float)vp[2] / (float)vp[3];
  gl4duFrustumf(-0.6f * aspect, 0.6f * aspect, -0.6f, 0.6f, 1.0f, 1000.0f); 

  gl4duBindMatrix("view");
  gl4duLoadIdentityf();
  gl4duLookAtf(camX, camY, camZ, tarX, tarY, tarZ, 0.0f, 1.0f, 0.0f);

  glUseProgram(_pIdCube);
  glUniform3f(glGetUniformLocation(_pIdCube, "color"), 1.0f, 0.8f, 0.1f);

  glUniform1f(glGetUniformLocation(_pIdCube, "time"), (float)t);

  if(t < 8.0f) { 
      static const char* text_map[] = {
        " XXXX  XXXXX XXXX  X   X XXXXX  XXXX ",
        " P   P E     R   R MM MM   I   S     ",
        " PPPP  EEE   RRRR  M M M   I    SSS  ",
        " P     E     R  R  M   M   I       S ",
        " P     XXXXX R   R M   M XXXXX XXXX  "
      };

      for(int y = 0; y < 5; y++) {
        for(int x = 0; x < 35; x++) {
          if(text_map[y][x] != ' ') {
            gl4duBindMatrix("model");
            gl4duLoadIdentityf();
            
            float posX = (x - 17.0f) * 0.15f;
            float posY = 6.0f - (y * 0.15f);
            float posZ = 0.0f; 
            
            gl4duTranslatef(posX, posY, posZ);
            gl4duRotatef(t * 50.0f + x * 10.0f, 1.0f, 1.0f, 0.0f);
            gl4duScalef(0.06f, 0.06f, 0.06f); 
            
            gl4duSendMatrices();
            gl4dgDraw(_cube);
          }
        }
      }
  }
  glUseProgram(0);
}