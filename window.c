/*!\file window.c
 *
 * \brief Utilisation de GL4Dummies pour réaliser une démo.
 */
#include <stdlib.h>
#include <GL4D/gl4du.h>
#include <GL4D/gl4dh.h>
#include <GL4D/gl4duw_SDL2.h>
#include "animations.h"
#include "audioHelper.h"

/* Prototypes des fonctions statiques contenues dans ce fichier C. */
static void init(void);
static void quit(void);
static void resize(int w, int h);
static void keydown(int keycode);
static void autoQuit(int state); 

/*!\brief tableau contenant les animations sous la forme de timeline */
static GL4DHanime _animations[] = {
  { 90000, b2b, NULL, NULL },       /* 90 secondes pour la voiture + ELIMINE + Crédits */
  { 100,   autoQuit, NULL, NULL },  /* Fermeture de la démo */
  { 0, NULL, NULL, NULL }           /* Fin de la timeline */
};

/*!\brief dimensions initiales de la fenêtre */
static GLfloat _dim[] = {1920, 1080};

int main(int argc, char ** argv) {
  if(!gl4duwCreateWindow(argc, argv, "L'Eveil de l'Anomalie - 64K", 
			 GL4DW_POS_UNDEFINED, GL4DW_POS_UNDEFINED, 
       _dim[0] / 2, _dim[1] / 2, 
             GL4DW_SHOWN))
    return 1;
    
  init();
  atexit(quit);
  gl4duwResizeFunc(resize);
  gl4duwKeyDownFunc(keydown);
  gl4duwDisplayFunc(gl4dhDraw);

  // Lancement de la musique
  ahInitAudio("mpiano.it"); 
  gl4duwMainLoop();
  return 0;
}

static void init(void) {
  int w, h;
  glClearColor(0.2f, 0.2f, 0.2f, 0.0f);
  gl4duwGetWindowSize(&w, &h);
  /* On initialise gl4dh avec la taille réelle de la fenêtre pour éviter le noir */
  gl4dhInit(_animations, w, h, animationsInit);
  resize(w, h);
}

static void resize(int w, int h) {
  glViewport(0, 0, w, h);
  gl4duBindMatrix("proj");
  gl4duLoadIdentityf();
  gl4duFrustumf(-(1.0f * w) / h, (1.0f * w) / h, -1.0f, 1.0f, 1.0f, 1000.0f);
}

static void keydown(int keycode) {
  switch(keycode) {
  case SDLK_ESCAPE:
  case 'q':
    exit(0);
  default: break;
  }
}

/*!\brief appelée pour forcer la fermeture de la démo à la fin */
static void autoQuit(int state) {
    /* Sécurité : on ferme uniquement quand c'est le moment de dessiner, 
       pas pendant l'initialisation de GL4Dummies ! */
    if(state & GL4DH_DRAW) {
        exit(0);
    }
}

static void quit(void) {
  ahClean();
  gl4duClean(GL4DU_ALL);
}