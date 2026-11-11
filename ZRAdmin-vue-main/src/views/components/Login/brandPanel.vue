<template>
  <div class="brand-panel" aria-hidden="true">
    <div class="glow g1"></div>
    <div class="glow g2"></div>
    <span class="petal p1"></span>
    <span class="petal p2"></span>
    <span class="petal p3"></span>
    <img class="brand-art" :src="art" alt="" />
    <p>银饰 · 手串 · 项链 · 檀木</p>
  </div>
</template>

<script setup name="brandPanel">
import art from '@/assets/logo/logo-transparent.png'
</script>

<style scoped lang="scss">
.brand-panel {
  position: relative;
  flex: 1 1 50%;
  min-height: 100vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background:
    radial-gradient(circle at 50% 42%, rgba(255, 255, 255, 0.72), transparent 34%),
    linear-gradient(160deg, #fff7f8 0%, #f8e4ea 48%, #f3d5df 100%);
  border-right: 1px solid rgba(196, 120, 138, 0.12);
}
.brand-art {
  position: relative;
  z-index: 2;
  width: min(58%, 400px);
  height: auto;
  object-fit: contain;
  filter: drop-shadow(0 18px 28px rgba(168, 98, 116, 0.16));
  animation: float 7s ease-in-out infinite;
}
p {
  position: relative;
  z-index: 2;
  margin: 8px 0 0;
  color: #a36878;
  letter-spacing: 4px;
  font-size: 13px;
}
.glow, .petal { position: absolute; pointer-events: none; }
.glow { border-radius: 50%; filter: blur(8px); }
.g1 { width: 220px; height: 220px; left: 12%; top: 16%; background: rgba(255, 255, 255, 0.45); animation: pulse 8s ease-in-out infinite; }
.g2 { width: 160px; height: 160px; right: 10%; bottom: 14%; background: rgba(255, 214, 224, 0.7); animation: pulse 9s ease-in-out infinite reverse; }
.petal { width: 12px; height: 16px; border-radius: 70% 10% 70% 10%; background: linear-gradient(160deg, rgba(255, 214, 224, 0.95), rgba(212, 140, 156, 0.45)); animation: fall 16s linear infinite; }
.p1 { left: 18%; }
.p2 { left: 62%; animation-duration: 19s; animation-delay: -6s; }
.p3 { left: 42%; animation-duration: 22s; animation-delay: -11s; }
@keyframes float { 50% { transform: translateY(-10px); } }
@keyframes pulse { 50% { transform: scale(1.08); opacity: 0.7; } }
@keyframes fall {
  0% { top: -8%; opacity: 0; transform: rotate(0); }
  12% { opacity: 0.8; }
  100% { top: 108%; opacity: 0; transform: rotate(210deg); }
}
@media (max-width: 900px) {
  .brand-panel { display: none; }
}
@media (prefers-reduced-motion: reduce) {
  .brand-art, .glow, .petal { animation: none; }
}
</style>
