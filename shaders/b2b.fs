#version 330
out vec4 fragColor;

uniform vec2 resolution;
uniform vec3 ro;   
uniform vec3 ta;   
uniform float time;

// 1. MATHÉMATIQUES ET OUTILS

vec2 opU2(vec2 d1, vec2 d2) { 
    return (d1.x < d2.x) ? d1 : d2; 
}

mat2 rot(float a) { 
    float c = cos(a), s = sin(a); 
    return mat2(c, -s, s, c); 
}

float hash11(float p) { 
    p = fract(p * .1031); 
    p *= p + 33.33; 
    p *= p + p; 
    return fract(p); 
}

mat3 setCamera(in vec3 ro, in vec3 ta, float cr) {
    vec3 cw = normalize(ta - ro);
    vec3 cp = vec3(sin(cr), cos(cr), 0.0);
    vec3 cu = normalize(cross(cw, cp));
    vec3 cv = normalize(cross(cu, cw));
    return mat3(cu, cv, cw);
}

// 2. PRIMITIVES 3D (SDF)

float udRoundBox(vec3 p, vec3 b, float r) { 
    return length(max(abs(p)-b,0.0))-r; 
}

float sdBox(vec3 p, vec3 b) { 
    vec3 d = abs(p)-b; 
    return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0)); 
}

float sdTorus(vec3 p, vec2 t) { 
    vec2 q = vec2(length(p.xz)-t.x,p.y); 
    return length(q)-t.y; 
}

float sdCylinder(vec3 p, vec2 h) { 
    vec2 d = abs(vec2(length(p.xz),p.y)) - h; 
    return min(max(d.x,d.y),0.0) + length(max(d,0.0)); 
}

float sdCone(vec3 p, vec2 c, float h) { 
    float q = length(p.xz); 
    return max(dot(c.xy,vec2(q,p.y)),-h-p.y); 
}

// 3. SCÈNE : LE TITRE "ELIMINE"

float mapElimine(vec3 p) {
    float d = 1000.0; vec3 q;
    q = p - vec3(-10.5, 0.0, 0.0);
    d = min(d, sdBox(q - vec3(-1.0, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, 2.2, 0.0), vec3(1.3, 0.3, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, 0.0, 0.0), vec3(1.0, 0.3, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, -2.2, 0.0), vec3(1.3, 0.3, 0.3)));
    
    q = p - vec3(-7.0, 0.0, 0.0);
    d = min(d, sdBox(q - vec3(-1.0, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, -2.2, 0.0), vec3(1.3, 0.3, 0.3)));
    
    q = p - vec3(-4.0, 0.0, 0.0); d = min(d, sdBox(q, vec3(0.3, 2.5, 0.3)));
    
    q = p - vec3(0.0, 0.0, 0.0);
    d = min(d, sdBox(q - vec3(-1.5, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    d = min(d, sdBox(q - vec3(1.5, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    vec3 mq1 = q - vec3(-0.8, 0.6, 0.0); mq1.xy = rot(-0.5) * mq1.xy; 
    d = min(d, sdBox(mq1, vec3(0.3, 1.8, 0.3)));
    vec3 mq2 = q - vec3(0.8, 0.6, 0.0); mq2.xy = rot(0.5) * mq2.xy; 
    d = min(d, sdBox(mq2, vec3(0.3, 1.8, 0.3)));
    
    q = p - vec3(4.0, 0.0, 0.0); d = min(d, sdBox(q, vec3(0.3, 2.5, 0.3)));
    
    q = p - vec3(7.5, 0.0, 0.0);
    d = min(d, sdBox(q - vec3(-1.2, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    d = min(d, sdBox(q - vec3(1.2, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    vec3 nq = q; nq.xy = rot(0.5) * nq.xy; 
    d = min(d, sdBox(nq, vec3(0.3, 2.8, 0.3)));
    
    q = p - vec3(11.5, 0.0, 0.0);
    d = min(d, sdBox(q - vec3(-1.0, 0.0, 0.0), vec3(0.3, 2.5, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, 2.2, 0.0), vec3(1.3, 0.3, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, 0.0, 0.0), vec3(1.0, 0.3, 0.3)));
    d = min(d, sdBox(q - vec3(0.0, -2.2, 0.0), vec3(1.3, 0.3, 0.3)));
    
    return d;
}

vec3 calcNormalElimine(vec3 pos) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        mapElimine(pos+e.xyy)-mapElimine(pos-e.xyy), 
        mapElimine(pos+e.yxy)-mapElimine(pos-e.yxy), 
        mapElimine(pos+e.yyx)-mapElimine(pos-e.yyx)
    ));
}

// 4. SCÈNE : LA VOITURE ET LE DÉCOR

vec3 cp = vec3(0.0, -0.3, -0.5); 

vec2 mapHabitacle(vec3 p, vec3 carPos) {
    vec2 res = vec2(1000.0, 0.0);
    vec3 localP = p - carPos;
    
    res = opU2(res, vec2(udRoundBox(localP - vec3(0.0, 0.1, -1.0), vec3(0.8, 0.15, 0.2), 0.05), 2.0));
    vec3 pw = localP - vec3(-0.4, 0.2, -0.7); pw.yz = rot(-0.6) * pw.yz;
    res = opU2(res, vec2(min(sdTorus(pw, vec2(0.18, 0.02)), sdBox(pw, vec3(0.15, 0.02, 0.02))), 3.0));
    
    float seatL = udRoundBox(localP - vec3(-0.4, -0.1, 0.1), vec3(0.25, 0.3, 0.15), 0.05);
    float seatR = udRoundBox(localP - vec3(0.4, -0.1, 0.1), vec3(0.25, 0.3, 0.15), 0.05);
    res = opU2(res, vec2(min(seatL, seatR), 6.0));
    
    vec3 pBrake = localP - vec3(0.0, 0.05, 0.15); 
    pBrake.yz = rot(0.7 - smoothstep(17.0, 17.5, time) * 0.7) * pBrake.yz;
    res = opU2(res, vec2(sdBox(pBrake - vec3(0.0, 0.0, -0.15), vec3(0.03, 0.02, 0.15)), 12.0));
    
    vec3 pe = localP - vec3(0.4, 0.2, 0.2);
    res = opU2(res, vec2(udRoundBox(pe, vec3(0.2, 0.25, 0.15), 0.08), 4.0)); 
    vec3 headPosE = vec3(0.4, 0.55, 0.15);
    vec3 phE = localP - headPosE;
    
    float r = (smoothstep(10.,10.2,time)-smoothstep(20.,20.5,time)) + (smoothstep(41.,41.2,time)-smoothstep(44.,44.5,time)) + (smoothstep(56.,56.2,time)-smoothstep(59.,59.5,time)) + (smoothstep(63.,63.2,time)-smoothstep(66.,66.2,time)) + smoothstep(68.,68.2,time);
    phE.xz = rot(r * -1.5) * phE.xz; 
    res = opU2(res, vec2(length(phE) - 0.13, 7.0));
    
    float eyesE = min(length(phE - vec3(-0.04, 0.02, -0.12)), length(phE - vec3(0.04, 0.02, -0.12))) - 0.015;
    res = opU2(res, vec2(eyesE, 11.0));
    
    vec3 ps = localP - vec3(-0.4, 0.2, 0.2);
    res = opU2(res, vec2(udRoundBox(ps, vec3(0.18, 0.25, 0.12), 0.08), 5.0));
    vec3 phS = localP - vec3(-0.4, 0.55, 0.15); phS.yz = rot(smoothstep(15.0, 15.5, time) * 0.8) * phS.yz;
    res = opU2(res, vec2(length(phS) - 0.12, 7.0)); 
    
    vec3 pcb = localP - vec3(0.3, 0.1, -0.1); pcb.xy = rot(-0.4) * pcb.xy;
    pcb.yz = rot(-0.7) * pcb.yz;
    res = opU2(res, vec2(sdBox(pcb, vec3(0.15, 0.2, 0.01)), 8.0));
    
    return res;
}

vec2 mapCarScene(vec3 p) {
    float speed = smoothstep(18.0, 22.0, time) * 12.0;
    float travel = max(0.0, time - 18.0) * speed;
    float impactBump = (time > 68.0 && time < 68.5) ? sin((time - 68.0) * 40.0) * 0.1 * (68.5 - time) * 2.0 : 0.0;
    float vib = (speed > 0.1) ? sin(time * 50.0) * 0.005 : 0.0;
    vec3 carPos = vec3(0.0, -0.3 + vib + impactBump, -0.5);

    vec2 res = vec2(1000.0, 0.0);
    vec3 pDecor = p; 
    pDecor.z -= travel; 

    // Tunnel
    vec3 pTunnel = pDecor - vec3(0.0, 1.5, -330.0);
    res = opU2(res, vec2(max(-sdBox(pTunnel, vec3(4.0, 2.5, 90.1)), sdBox(pTunnel, vec3(4.5, 3.0, 90.0))), 13.0));
    
    // Piéton
    vec3 pCross = pDecor - vec3(0.0, 0.0, -600.0);
    if (abs(pCross.z) < 2.0 && abs(pCross.x) < 4.0) {
        res = opU2(res, vec2(sdBox(vec3(mod(pCross.x, 1.0) - 0.5, pCross.y + 0.79, pCross.z), vec3(0.3, 0.01, 2.0)), 8.0));
    }
    if (time < 68.1) {
        vec3 pPed = pCross; 
        pPed.x -= 4.5 - max(0.0, time - 60.0) * 0.56; 
        pPed.y += 0.8;
        res = opU2(res, vec2(min(udRoundBox(pPed - vec3(0.0, 0.6, 0.0), vec3(0.15, 0.5, 0.15), 0.05), length(pPed - vec3(0.0, 1.4, 0.0)) - 0.2), 15.0));
    }

    // Décor : Végétation et Bâtiments
    bool inTunnelZone = abs(pTunnel.z) < 90.0;
    res = opU2(res, vec2(p.y + 0.8, 1.0)); 
    res = opU2(res, vec2(sdBox(vec3(p.x, p.y + 0.79, mod(pDecor.z, 6.0) - 3.0), vec3(0.06, 0.01, 1.2)), 8.0));
    
    if (!inTunnelZone) {
        float cellSize = 8.0; 
        float idZ = floor(pDecor.z / cellSize);
        float rand = hash11(idZ);
        vec3 pScatter = p; 
        pScatter.z = mod(pDecor.z, cellSize) - cellSize * 0.5;
        
        if (abs(pDecor.z - (-600.0)) > 5.0) {
            pScatter.x += ((hash11(idZ + 1.0) > 0.5) ? 1.0 : -1.0) * (6.0 + hash11(idZ + 2.0) * 10.0);
            if(rand < 0.3) { 
                res = opU2(res, vec2(sdBox(pScatter - vec3(0.0, 15.0*rand+7.5, 0.0), vec3(2.5, 15.0*rand+7.5, 2.5)), 13.0));
            } else { 
                float hTrunk = 1.0 + rand * 1.0;
                vec3 pFoliage = pScatter - vec3(0.0, hTrunk * 2.0 + 0.5, 0.0);
                float tree = min(sdCylinder(pScatter - vec3(0.0, hTrunk, 0.0), vec2(0.1, hTrunk)), 
                                 min(sdCone(pFoliage, vec2(0.8, 2.0), 1.8), sdCone(pFoliage - vec3(0,0.8,0), vec2(0.6, 2.0), 1.2)));
                res = opU2(res, vec2(tree, 14.0));
            }
        }
    }

    // Voiture
    res = opU2(res, mapHabitacle(p, carPos));
    vec3 lp = p - carPos;
    float shell = min(udRoundBox(lp, vec3(0.9, 0.25, 1.8), 0.1), udRoundBox(lp - vec3(0.0, 0.4, -0.2), vec3(0.7, 0.25, 0.9), 0.1));
    shell = max(-udRoundBox(lp - vec3(0.0, 0.3, -0.2), vec3(0.81, 0.36, 1.41), 0.1), shell);
    shell = max(-sdBox(lp - vec3(0.0, 0.45, -0.2), vec3(1.0, 0.15, 0.7)), shell);
    res = opU2(res, vec2(max(-sdBox(lp - vec3(0.0, 0.45, -1.0), vec3(0.6, 0.15, 0.4)), max(-sdBox(lp - vec3(0.0, 0.45, 0.6), vec3(0.6, 0.15, 0.4)), shell)), 9.0));
    
    return res;
}

vec3 calcNormalCar(vec3 pos) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        mapCarScene(pos+e.xyy).x-mapCarScene(pos-e.xyy).x, 
        mapCarScene(pos+e.yxy).x-mapCarScene(pos-e.yxy).x, 
        mapCarScene(pos+e.yyx).x-mapCarScene(pos-e.yyx).x
    ));
}

float calcShadow(vec3 ro, vec3 rd) {
    float res = 1.0; 
    float t = 0.05;
    for(int i = 0; i < 30; i++) {
        float h = mapCarScene(ro + rd * t).x;
        if(h < 0.001) return 0.1; 
        res = min(res, 8.0 * h / t); 
        t += h;
        if(t > 20.0) break;
    }
    return clamp(res, 0.1, 1.0);
}

float calcRain(vec2 uv, float time) {
    float rainStr = 0.0;
    for (float i = 1.0; i < 5.0; i++) {
        vec2 st = uv * vec2(50.0 / i, 2.0 / i);
        st.x += time * 2.0 * i; 
        st.y += time * 15.0 * i;
        vec2 id = floor(st); 
        vec2 p = fract(st);
        float n = hash11(id.x * 123.4 + id.y * 321.4);
        if (n > 0.95) {
            float drop = smoothstep(0.95, 1.0, 1.0 - length(p - vec2(0.5, 0.5)));
            rainStr += drop * (n - 0.95) * 20.0 / i;
        }
    }
    return clamp(rainStr, 0.0, 1.0);
}

// 6. MOTEUR TYPOGRAPHIQUE STRICT (FONT SYSTEM)

float L(vec3 p, int id) {
    float d = 1000.0;
    vec3 v = vec3(0.08, 1.0, 0.08); // Barre verticale
    vec3 h = vec3(0.3, 0.08, 0.08); // Barre horizontale
    vec3 halfV = vec3(0.08, 0.5, 0.08); // Demi-barre verticale

    if(id==1) { // A
        d = min(sdBox(p-vec3(-0.4,0,0),v), sdBox(p-vec3(0.4,0,0),v));
        d = min(d, min(sdBox(p-vec3(0,1,0),h), sdBox(p,h)));
    }
    else if(id==2) { // B
        d = min(sdBox(p-vec3(-0.4,0,0),v), sdBox(p-vec3(0.4,0,0),v));
        d = min(d, min(sdBox(p-vec3(0,1,0),h), min(sdBox(p,h), sdBox(p-vec3(0,-1,0),h))));
    }
    else if(id==3) { // D
        d = min(sdBox(p-vec3(-0.4,0,0),v), sdBox(p-vec3(0.4,0,0),v));
        d = min(d, min(sdBox(p-vec3(0,1,0),h), sdBox(p-vec3(0,-1,0),h)));
    }
    else if(id==4) { // E
        d = sdBox(p-vec3(-0.4,0,0),v);
        d = min(d, min(sdBox(p-vec3(0,1,0),h), min(sdBox(p,h), sdBox(p-vec3(0,-1,0),h))));
    }
    else if(id==5) { // F
        d = sdBox(p-vec3(-0.4,0,0),v);
        d = min(d, min(sdBox(p-vec3(0,1,0),h), sdBox(p,h)));
    }
    else if(id==6) { // H
        d = min(sdBox(p-vec3(-0.4,0,0),v), sdBox(p-vec3(0.4,0,0),v));
        d = min(d, sdBox(p,h));
    }
    else if(id==7) { // I
        d = sdBox(p,v);
    }
    else if(id==8) { // J
        d = min(sdBox(p-vec3(0.4,0,0),v), sdBox(p-vec3(0,-1,0),h));
        d = min(d, sdBox(p-vec3(-0.4,-0.5,0), halfV));
    }
    else if(id==9) { // L
        d = min(sdBox(p-vec3(-0.4,0,0),v), sdBox(p-vec3(0,-1,0),h));
    }
    else if(id==10) { // M
        d = min(sdBox(p-vec3(-0.4,0,0),v), sdBox(p-vec3(0.4,0,0),v));
        d = min(d, sdBox(p-vec3(0, 0.2, 0), vec3(0.2, 0.8, 0.08))); 
    }
    else if(id==11) { // R
        d = sdBox(p-vec3(-0.4,0,0),v);
        d = min(d, min(sdBox(p-vec3(0,1,0),h), sdBox(p,h)));
        d = min(d, sdBox(p-vec3(0.4,0.5,0), halfV));
        d = min(d, sdBox(p-vec3(0.3,-0.5,0), vec3(0.15, 0.6, 0.08)));
    }
    else if(id==12) { // S
        d = min(sdBox(p-vec3(0,1,0),h), min(sdBox(p,h), sdBox(p-vec3(0,-1,0),h)));
        d = min(d, min(sdBox(p-vec3(-0.4,0.5,0),halfV), sdBox(p-vec3(0.4,-0.5,0),halfV)));
    }
    else if(id==13) { // T
        d = min(sdBox(p-vec3(0,1,0),vec3(0.5,0.08,0.08)), sdBox(p,v));
    }
    else if(id==14) { // Y
        d = min(sdBox(p-vec3(-0.4,0.5,0),halfV), sdBox(p-vec3(0.4,0.5,0),halfV));
        d = min(d, min(sdBox(p,h), sdBox(p-vec3(0,-0.5,0),halfV)));
    }
    else if(id==15) { // 3
        d = min(sdBox(p-vec3(0,1,0),h), min(sdBox(p,h), sdBox(p-vec3(0,-1,0),h)));
        d = min(d, sdBox(p-vec3(0.4,0,0),v));
    }
    else if(id==16) { // -
        d = sdBox(p,h);
    }
    return d;
}

// 7. AGENCEMENT MILLIMÉTRÉ DES MOTS

float mapCredits(vec3 p, int stage) {
    float d = 1000.0;
    
    if (stage == 0) { 
        // Ligne HAUT : MATHIS (M=10, A=1, T=13, H=6, I=7, S=12)
        vec3 p1 = p - vec3(0, 1.5, 0);
        d = min(d, L(p1-vec3(-3.75,0,0), 10)); 
        d = min(d, L(p1-vec3(-2.25,0,0), 1)); 
        d = min(d, L(p1-vec3(-0.75,0,0), 13)); 
        d = min(d, L(p1-vec3( 0.75,0,0), 6)); 
        d = min(d, L(p1-vec3( 2.25,0,0), 7)); 
        d = min(d, L(p1-vec3( 3.75,0,0), 12)); 
        
        // Ligne BAS : HAMRI (H=6, A=1, M=10, R=11, I=7)
        vec3 p2 = p - vec3(0, -1.5, 0);
        d = min(d, L(p2-vec3(-3.0,0,0), 6)); 
        d = min(d, L(p2-vec3(-1.5,0,0), 1)); 
        d = min(d, L(p2-vec3( 0.0,0,0), 10)); 
        d = min(d, L(p2-vec3( 1.5,0,0), 11)); 
        d = min(d, L(p2-vec3( 3.0,0,0), 7)); 
    }
    else if (stage == 1) { 
        // Ligne CENTRE : L3-Y (L=9, 3=15, -=16, Y=14)
        d = min(d, L(p-vec3(-2.25,0,0), 9)); 
        d = min(d, L(p-vec3(-0.75,0,0), 15)); 
        d = min(d, L(p-vec3( 0.75,0,0), 16)); 
        d = min(d, L(p-vec3( 2.25,0,0), 14)); 
    }
    else { 
        // Ligne HAUT : FARES (F=5, A=1, R=11, E=4, S=12)
        vec3 p1 = p - vec3(0, 1.5, 0);
        d = min(d, L(p1-vec3(-3.0,0,0), 5)); 
        d = min(d, L(p1-vec3(-1.5,0,0), 1));  
        d = min(d, L(p1-vec3( 0.0,0,0), 11));  
        d = min(d, L(p1-vec3( 1.5,0,0), 4)); 
        d = min(d, L(p1-vec3( 3.0,0,0), 12));  
        
        // Ligne BAS : BELHADJ (B=2, E=4, L=9, H=6, A=1, D=3, J=8)
        vec3 p2 = p - vec3(0, -1.5, 0);
        d = min(d, L(p2-vec3(-4.5,0,0), 2)); 
        d = min(d, L(p2-vec3(-3.0,0,0), 4)); 
        d = min(d, L(p2-vec3(-1.5,0,0), 9)); 
        d = min(d, L(p2-vec3( 0.0,0,0), 6));  
        d = min(d, L(p2-vec3( 1.5,0,0), 1));  
        d = min(d, L(p2-vec3( 3.0,0,0), 3)); 
        d = min(d, L(p2-vec3( 4.5,0,0), 8)); 
    }
    return d;
}

vec3 calcNormalCredits(vec3 pos, int stage) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        mapCredits(pos+e.xyy, stage)-mapCredits(pos-e.xyy, stage), 
        mapCredits(pos+e.yxy, stage)-mapCredits(pos-e.yxy, stage), 
        mapCredits(pos+e.yyx, stage)-mapCredits(pos-e.yyx, stage)
    ));
}

// 8. RENDU PRINCIPAL DU SHADER

void main() {
    if (time > 90.0) { fragColor = vec4(0.0, 0.0, 0.0, 1.0); return; }
    
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution.xy) / resolution.y;
    
    // SCÈNE 3 : CRÉDITS (80s à 90s)

    if (time > 80.0) {
        float tCred = time - 80.0; 
        
        int stage = 0;
        float localT = tCred;
        
        // 3 étapes de 3.33 secondes pour laisser le temps de lire
        if (tCred > 6.66) { stage = 2; localT = tCred - 6.66; }
        else if (tCred > 3.33) { stage = 1; localT = tCred - 3.33; }
        
        // Caméra très reculée (Z=26) pour que tout rentre à l'écran
        vec3 camRo = vec3(0.0, 0.0, 26.0 - localT * 1.5); 
        vec3 camTa = vec3(0.0, 0.0, 0.0);
        
        mat3 cam = setCamera(camRo, camTa, 0.0);
        vec3 rd = cam * normalize(vec3(uv, 1.0)); 
        
        float t = 0.0; vec3 p;
        for(int i = 0; i < 60; i++) {
            p = camRo + rd * t; 
            float d = mapCredits(p, stage);
            if(d < 0.001 || t > 40.0) break;
            t += d;
        }
        
        vec3 col = vec3(0.0);
        if(t < 40.0) {
            vec3 n = calcNormalCredits(p, stage);
            float diff = max(dot(n, normalize(vec3(0.5, 1.0, 0.8))), 0.0);
            
            // Professeur en Or, moi en Gris Argenté
            vec3 matCol = (stage == 2) ? vec3(1.0, 0.8, 0.1) : vec3(0.8, 0.8, 0.85); 
            col = matCol * (diff * 0.8 + 0.2); 
        }
        
        col += calcRain(uv, time) * 0.15; 
        
        // Fondu d'apparition et de disparition pour chaque étape
        col *= smoothstep(0.0, 0.5, localT) * (1.0 - smoothstep(2.8, 3.33, localT)); 
        
        fragColor = vec4(pow(col, vec3(0.4545)), 1.0);
        return;
    }

    // SCÈNE 2 : L'ÉCRAN DE FIN - ELIMINE (73.0s à 80.0s)

    if (time > 73.0) {
        float tScene = time - 73.0; 
        float fadeOut = 1.0 - smoothstep(6.5, 7.0, tScene); 
        
        vec3 camRo = vec3(0.0, mix(40.0, 0.0, smoothstep(0.0, 3.0, tScene)), 22.0); 
        mat3 cam = setCamera(camRo, vec3(0.0), 0.0);
        vec3 rd = cam * normalize(vec3(uv, 1.0)); 
        
        float t = 0.0; vec3 p;
        for(int i = 0; i < 80; i++) {
            p = camRo + rd * t; 
            float d = mapElimine(p);
            if(d < 0.001 || t > 60.0) break;
            t += d;
        }
        
        vec3 col = vec3(0.0);
        float rotSpeed = time * 12.0; 
        
        if(t < 60.0) {
            vec3 n = calcNormalElimine(p);
            float diff = max(dot(n, normalize(vec3(0.5, 1.0, 0.8))), 0.0);
            vec3 blueL = vec3(0.1, 0.3, 1.0) * max(dot(n, normalize(vec3(-8,3,6)-p)), 0.0) * 2.0;
            vec3 redL = vec3(1.0, 0.1, 0.1) * max(dot(n, normalize(vec3(8,3,6)-p)), 0.0) * 2.0;
            col = vec3(0.6, 0.02, 0.02) * (diff + 0.1) + blueL + redL;
        } else {
            float bgBlue = 0.8 / (length(uv - vec2(-0.6, 0.0)) + 0.5) * max(0.0, sin(rotSpeed));
            float bgRed  = 0.8 / (length(uv - vec2( 0.6, 0.0)) + 0.5) * max(0.0, sin(rotSpeed + 3.14));
            col = vec3(bgRed, 0.0, bgBlue) * 0.12; 
        }
        
        col += calcRain(uv, time) * 0.3; 
        col *= fadeOut;
        
        fragColor = vec4(pow(col, vec3(0.4545)), 1.0);
        return; 
    }

    // SCÈNE 1 : LA VOITURE ET LE DÉCOR (0s à 73s)

    if (time > 68.0 && time < 68.6) { 
        uv += sin(time * 150.0) * (68.6 - time) * 0.03; 
    }
    
    mat3 cam = setCamera(ro, ta, 0.0);
    vec3 rd = cam * normalize(vec3(uv, 1.0)); 
    
    float t = 0.0, mat = 0.0; vec3 p;
    for(int i = 0; i < 150; i++) {
        p = ro + rd * t; 
        vec2 d = mapCarScene(p);
        if(d.x < 0.001 || t > 150.0) break;
        t += d.x; 
        mat = d.y;
    }
    
    vec3 col = vec3(0.03, 0.06, 0.1);
    
    if(t < 150.0) {
        vec3 n = calcNormalCar(p);
        vec3 lightDir = normalize(vec3(0.5, 1.0, 0.5));
        
        float speed = smoothstep(18.0, 22.0, time) * 12.0;
        float travel = max(0.0, time - 18.0) * speed;
        float inTunnel = smoothstep(230.0, 240.0, travel) - smoothstep(410.0, 420.0, travel);
        
        float shadow = mix(calcShadow(p, lightDir), 1.0, inTunnel);
        float diff = max(dot(n, lightDir), 0.0) * (1.0 - inTunnel * 0.95);
        
        vec3 matCol = vec3(1.0);
        if(mat == 1.0) matCol = vec3(0.08);
        else if(mat == 4.0) matCol = vec3(0.15, 0.15, 0.18); 
        else if(mat == 5.0) matCol = vec3(0.1, 0.3, 0.5);
        else if(mat == 7.0) matCol = vec3(0.8, 0.6, 0.5); 
        else if(mat == 8.0) matCol = vec3(0.9);
        else if(mat == 9.0) matCol = vec3(0.8, 0.1, 0.1); 
        else if(mat == 11.0) matCol = vec3(0.0);
        else if(mat == 12.0) matCol = vec3(0.02); 
        else if(mat == 13.0) matCol = vec3(0.25);
        else if(mat == 14.0) { matCol = (p.y > 2.0) ? vec3(0.05, 0.15, 0.05) : vec3(0.2, 0.1, 0.05); }
        else if(mat == 15.0) matCol = vec3(0.9, 0.8, 0.1);
        
        vec3 baseLighting = matCol * (diff * shadow + mix(0.1 + 0.1 * n.y, 0.01, inTunnel));
        
        float lightsOn = smoothstep(43.0, 43.2, time) - smoothstep(58.0, 58.2, time);
        if (lightsOn > 0.0) {
            vec3 toP = p - vec3(0.0, -0.1, -1.8);
            float dist = length(toP);
            vec3 pDir = normalize(toP);
            baseLighting += matCol * vec3(1.0, 0.95, 0.8) * 4.0 * max(dot(n, -pDir), 0.0) * smoothstep(0.8, 0.98, max(dot(pDir, vec3(0.0, 0.0, -1.0)), 0.0)) * (1.0 / (1.0 + 0.02 * dist + 0.002 * dist * dist)) * lightsOn;
        }
        
        col = mix(baseLighting, vec3(0.03, 0.06, 0.1), 1.0 - exp(-0.007 * t));
    }
    
    float rain = calcRain(uv, time);
    float speed = smoothstep(18.0, 22.0, time) * 12.0;
    float inTunnel = smoothstep(230.0, 240.0, max(0.0, time - 18.0) * speed) - smoothstep(410.0, 420.0, max(0.0, time - 18.0) * speed);
    col += rain * 0.4 * (1.0 - inTunnel); 

    if (time > 68.0 && time < 68.6) {
        float flash = exp(-(time - 68.0) * 10.0); 
        col += vec3(flash * 0.8);
        if (step(0.9, sin(uv.y * 50.0 + time * 100.0)) > 0.0 && hash11(time * 50.0 + uv.y) > 0.5) { 
            col.r = 1.0 - col.r; col.g += 0.2; 
        }
        col = mix(col, vec3(0.8, 0.0, 0.0), length(uv) * 1.5 * flash);
    }
    
    col *= 1.0 - smoothstep(72.5, 73.0, time);
    fragColor = vec4(pow(col, vec3(0.4545)), 1.0);
}
