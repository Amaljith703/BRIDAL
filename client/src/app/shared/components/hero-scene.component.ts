import {
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import * as THREE from 'three';

/**
 * A cinematic 3D layer for the home hero: silk ribbon, drifting petals and a
 * slow orbital light. Ambient, pointer-reactive and fully disposable.
 */
@Component({
  selector: 'app-hero-scene',
  template: `<canvas #canvas aria-hidden="true"></canvas>`,
  styles: `
    :host {
      position: absolute;
      inset: 0;
      display: block;
      pointer-events: none;
    }
    canvas {
      width: 100%;
      height: 100%;
      display: block;
    }
  `,
})
export class HeroSceneComponent implements OnDestroy {
  readonly accent = input('#e9c8c1');
  readonly secondary = input('#c9a26b');
  readonly deep = input('#5e2e3c');

  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private renderer?: THREE.WebGLRenderer;
  private scene?: THREE.Scene;
  private camera?: THREE.PerspectiveCamera;
  private frame = 0;
  private paused = false;
  private reduced = false;
  private clock?: THREE.Clock;
  private ribbon?: THREE.Mesh<THREE.PlaneGeometry, THREE.ShaderMaterial>;
  private petals?: THREE.Points<THREE.BufferGeometry, THREE.PointsMaterial>;
  private halo?: THREE.Mesh<THREE.TorusGeometry, THREE.MeshBasicMaterial>;
  private orb?: THREE.Mesh<THREE.SphereGeometry, THREE.MeshStandardMaterial>;
  private observer?: ResizeObserver;
  private visibility?: () => void;

  private readonly pointer = { x: 0, y: 0 };
  private readonly target = { x: 0, y: 0 };
  private readonly onPointerMove = (event: PointerEvent) => {
    const width = window.innerWidth || 1;
    const height = window.innerHeight || 1;
    this.target.x = (event.clientX / width) * 2 - 1;
    this.target.y = (event.clientY / height) * 2 - 1;
  };

  constructor() {
    afterNextRender(() => this.init());

    effect(() => {
      const accent = this.accent();
      const secondary = this.secondary();
      const deep = this.deep();
      const ribbon = this.ribbon;
      if (!ribbon) return;
      ribbon.material.uniforms['uAccent'].value = new THREE.Color(accent);
      ribbon.material.uniforms['uSecondary'].value = new THREE.Color(secondary);
      ribbon.material.uniforms['uDeep'].value = new THREE.Color(deep);
    });
  }

  private init(): void {
    const canvas = this.canvasRef()?.nativeElement;
    if (!canvas) return;
    this.reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

    try {
      this.renderer = new THREE.WebGLRenderer({
        canvas,
        alpha: true,
        antialias: true,
        powerPreference: 'high-performance',
      });
    } catch {
      return;
    }

    const width = this.host.nativeElement.clientWidth || window.innerWidth;
    const height = this.host.nativeElement.clientHeight || window.innerHeight;
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.8));
    this.renderer.setSize(width, height, false);
    this.renderer.setClearColor(0x000000, 0);

    this.scene = new THREE.Scene();
    this.scene.fog = new THREE.FogExp2(0x2a161c, 0.075);
    this.camera = new THREE.PerspectiveCamera(46, width / height, 0.1, 60);
    this.camera.position.set(0, 0.4, 8.4);
    this.clock = new THREE.Clock();

    this.buildRibbon();
    this.buildPetals();
    this.buildOrbitals();
    this.buildLights();

    window.addEventListener('pointermove', this.onPointerMove, { passive: true });
    this.observer = new ResizeObserver(() => this.resize());
    this.observer.observe(this.host.nativeElement);
    const onVisibility = () => {
      this.paused = document.hidden;
      if (!this.paused) this.loop();
    };
    document.addEventListener('visibilitychange', onVisibility);
    this.visibility = () => document.removeEventListener('visibilitychange', onVisibility);

    this.loop();
  }

  private buildRibbon(): void {
    if (!this.scene) return;
    const geometry = new THREE.PlaneGeometry(14, 8, 130, 74);
    const material = new THREE.ShaderMaterial({
      transparent: true,
      side: THREE.DoubleSide,
      depthWrite: false,
      uniforms: {
        uTime: { value: 0 },
        uPointer: { value: new THREE.Vector2(0, 0) },
        uAccent: { value: new THREE.Color(this.accent()) },
        uSecondary: { value: new THREE.Color(this.secondary()) },
        uDeep: { value: new THREE.Color(this.deep()) },
      },
      vertexShader: `
        uniform float uTime;
        uniform vec2 uPointer;
        varying vec2 vUv;
        varying float vWave;

        void main() {
          vUv = uv;
          vec3 pos = position;
          float wave = sin(pos.x * 0.72 + uTime * 0.42) * 0.62;
          wave += cos(pos.y * 0.58 - uTime * 0.33) * 0.44;
          wave += sin((pos.x + pos.y) * 0.4 + uTime * 0.2) * 0.3;
          float pinch = smoothstep(0.0, 1.0, 1.0 - abs(uv.x - 0.5) * 1.6);
          pos.z += wave * pinch;
          pos.x += uPointer.x * 0.35 * pinch;
          pos.y += uPointer.y * 0.22 * pinch;
          vWave = wave;
          gl_Position = projectionMatrix * modelViewMatrix * vec4(pos, 1.0);
        }
      `,
      fragmentShader: `
        uniform vec3 uAccent;
        uniform vec3 uSecondary;
        uniform vec3 uDeep;
        varying vec2 vUv;
        varying float vWave;

        void main() {
          float edge = smoothstep(0.0, 0.32, vUv.x) * smoothstep(1.0, 0.68, vUv.x);
          float grain = sin(vUv.y * 220.0 + vWave * 6.0) * 0.5 + 0.5;
          vec3 color = mix(uDeep, uAccent, smoothstep(0.06, 1.0, vUv.y + vWave * 0.16));
          color = mix(color, uSecondary, smoothstep(0.55, 1.0, grain) * 0.22);
          float alpha = edge * (0.14 + grain * 0.12);
          gl_FragColor = vec4(color, alpha);
        }
      `,
    });

    const ribbon = new THREE.Mesh(geometry, material);
    ribbon.rotation.x = -0.86;
    ribbon.rotation.z = 0.16;
    ribbon.position.set(1.4, -0.4, -2.6);
    this.ribbon = ribbon;
    this.scene.add(ribbon);
  }

  private buildPetals(): void {
    if (!this.scene) return;
    const count = window.innerWidth < 760 ? 260 : 640;
    const positions = new Float32Array(count * 3);
    const seeds = new Float32Array(count);
    for (let i = 0; i < count; i += 1) {
      positions[i * 3] = (Math.random() - 0.5) * 17;
      positions[i * 3 + 1] = (Math.random() - 0.5) * 10;
      positions[i * 3 + 2] = (Math.random() - 0.5) * 9 - 1.5;
      seeds[i] = Math.random();
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));

    const material = new THREE.PointsMaterial({
      color: new THREE.Color(this.accent()),
      size: 0.055,
      sizeAttenuation: true,
      transparent: true,
      opacity: 0.62,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      map: this.petalTexture(),
    });

    const petals = new THREE.Points(geometry, material);
    petals.userData['seeds'] = seeds;
    this.petals = petals;
    this.scene.add(petals);
  }

  private petalTexture(): THREE.Texture {
    const size = 64;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = size;
    const context = canvas.getContext('2d');
    if (context) {
      const gradient = context.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
      gradient.addColorStop(0, 'rgba(255,255,255,0.95)');
      gradient.addColorStop(0.4, 'rgba(255,240,235,0.55)');
      gradient.addColorStop(1, 'rgba(255,255,255,0)');
      context.fillStyle = gradient;
      context.fillRect(0, 0, size, size);
    }
    const texture = new THREE.CanvasTexture(canvas);
    texture.needsUpdate = true;
    return texture;
  }

  private buildOrbitals(): void {
    if (!this.scene) return;
    const halo = new THREE.Mesh(
      new THREE.TorusGeometry(2.9, 0.008, 8, 160),
      new THREE.MeshBasicMaterial({
        color: new THREE.Color(this.secondary()),
        transparent: true,
        opacity: 0.5,
        blending: THREE.AdditiveBlending,
      }),
    );
    halo.position.set(2.6, 0.9, -1.2);
    halo.rotation.set(1.05, 0.3, 0);
    this.halo = halo;
    this.scene.add(halo);

    const orb = new THREE.Mesh(
      new THREE.SphereGeometry(0.72, 48, 48),
      new THREE.MeshStandardMaterial({
        color: new THREE.Color(this.deep()),
        roughness: 0.18,
        metalness: 0.35,
        transparent: true,
        opacity: 0.5,
      }),
    );
    orb.position.set(3.1, 1.15, 0.4);
    this.orb = orb;
    this.scene.add(orb);
  }

  private buildLights(): void {
    if (!this.scene) return;
    this.scene.add(new THREE.AmbientLight(0xf7e2dc, 1.05));
    const key = new THREE.DirectionalLight(0xffd9c9, 2.1);
    key.position.set(4, 5, 6);
    this.scene.add(key);
    const rim = new THREE.PointLight(0xc9a26b, 18, 24, 2);
    rim.position.set(-5, 1.5, 2);
    this.scene.add(rim);
  }

  private resize(): void {
    if (!this.renderer || !this.camera) return;
    const width = this.host.nativeElement.clientWidth || window.innerWidth;
    const height = this.host.nativeElement.clientHeight || window.innerHeight;
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / Math.max(height, 1);
    this.camera.updateProjectionMatrix();
  }

  private loop = (): void => {
    if (!this.renderer || !this.scene || !this.camera || !this.clock) return;
    cancelAnimationFrame(this.frame);
    if (this.paused || this.reduced) {
      if (this.reduced) this.renderer.render(this.scene, this.camera);
      return;
    }
    this.frame = requestAnimationFrame(this.loop);
    const time = this.clock.getElapsedTime();

    this.pointer.x += (this.target.x - this.pointer.x) * 0.04;
    this.pointer.y += (this.target.y - this.pointer.y) * 0.04;

    if (this.ribbon) {
      this.ribbon.material.uniforms['uTime'].value = time;
      this.ribbon.material.uniforms['uPointer'].value.set(this.pointer.x, this.pointer.y);
      this.ribbon.rotation.z = 0.16 + Math.sin(time * 0.14) * 0.04;
    }

    if (this.petals) {
      const positions = this.petals.geometry.getAttribute('position') as THREE.BufferAttribute;
      const array = positions.array as Float32Array;
      const seeds = this.petals.userData['seeds'] as Float32Array;
      for (let i = 0; i < positions.count; i += 1) {
        const seed = seeds[i];
        array[i * 3] += Math.sin(time * 0.2 + seed * 12) * 0.0009;
        array[i * 3 + 1] += 0.0022 + seed * 0.0045;
        if (array[i * 3 + 1] > 5.4) array[i * 3 + 1] = -5.4;
      }
      positions.needsUpdate = true;
      this.petals.rotation.y = time * 0.012 + this.pointer.x * 0.08;
    }

    if (this.halo) {
      this.halo.rotation.z = time * 0.09;
      this.halo.rotation.x = 1.05 + Math.sin(time * 0.22) * 0.12;
      this.halo.position.y = 0.9 + Math.sin(time * 0.4) * 0.14;
    }
    if (this.orb) {
      this.orb.position.y = 1.15 + Math.sin(time * 0.5 + 1.2) * 0.18;
      this.orb.rotation.y = time * 0.2;
    }

    this.camera.position.x = this.pointer.x * 0.55;
    this.camera.position.y = 0.4 - this.pointer.y * 0.32;
    this.camera.lookAt(1.1, 0.25, 0);

    this.renderer.render(this.scene, this.camera);
  };

  ngOnDestroy(): void {
    cancelAnimationFrame(this.frame);
    window.removeEventListener('pointermove', this.onPointerMove);
    this.observer?.disconnect();
    this.visibility?.();
    this.scene?.traverse((object) => {
      const mesh = object as THREE.Mesh;
      mesh.geometry?.dispose?.();
      const material = mesh.material as THREE.Material | THREE.Material[] | undefined;
      if (Array.isArray(material)) material.forEach((item) => item.dispose());
      else material?.dispose();
    });
    this.petals?.material.map?.dispose();
    this.renderer?.dispose();
  }
}
