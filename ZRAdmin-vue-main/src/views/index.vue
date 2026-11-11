<template>
  <div class="home-carousel" @touchstart.passive="onTouchStart" @touchend.passive="onTouchEnd">
    <div class="carousel-viewport">
      <div class="carousel-track">
        <section class="carousel-page" :class="{ active: activePage === 0, 'slide-next': activePage === 0 && direction > 0, 'slide-prev': activePage === 0 && direction < 0 }" :aria-hidden="activePage !== 0">
          <PerformanceTargetBreakdown />
        </section>
        <section class="carousel-page" :class="{ active: activePage === 1, 'slide-next': activePage === 1 && direction > 0, 'slide-prev': activePage === 1 && direction < 0 }" :aria-hidden="activePage !== 1">
          <ConsolePage />
        </section>
      </div>
    </div>

    <nav class="page-switcher" aria-label="首页视图切换">
      <button v-for="(page, index) in pages" :key="page.key" :class="{ active: activePage === index }" @click="goPage(index)">
        <i></i><span>{{ page.label }}</span>
      </button>
    </nav>
  </div>
</template>

<script setup name="Index">
import { nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import PerformanceTargetBreakdown from './jingyue/home/PerformanceTargetBreakdown.vue'
import ConsolePage from './dashboard/ConsolePage.vue'

const pages = [
  { key: 'breakdown', label: '业绩目标拆解' },
  { key: 'console', label: '经营控制台' }
]
const activePage = ref(0)
const direction = ref(1)
const touch = reactive({ startX: 0, startY: 0, tracking: false })

function scrollTop() {
  // 布局真正的滚动容器是 .app-main（overflow-y: auto），window 本身不滚动
  const scroller = document.querySelector('.app-main')
  if (scroller && scroller.scrollTop > 0) scroller.scrollTo({ top: 0, behavior: 'smooth' })
  if (window.scrollY > 0) window.scrollTo({ top: 0, behavior: 'smooth' })
  if (document.documentElement.scrollTop > 0) document.documentElement.scrollTop = 0
}

function goPage(index) {
  if (index < 0 || index >= pages.length || index === activePage.value) return
  direction.value = index > activePage.value ? 1 : -1
  activePage.value = index
  // 切换后立刻回到顶部，避免第二页停留在上一页的滚动位置
  scrollTop()
  nextTick(() => {
    scrollTop()
    requestAnimationFrame(scrollTop)
  })
}
function goPrev() { goPage(activePage.value - 1) }
function goNext() { goPage(activePage.value + 1) }

function onTouchStart(event) {
  const point = event.changedTouches?.[0]
  if (!point) return
  touch.startX = point.clientX
  touch.startY = point.clientY
  touch.tracking = true
}
function onTouchEnd(event) {
  if (!touch.tracking) return
  touch.tracking = false
  const point = event.changedTouches?.[0]
  if (!point) return
  const dx = point.clientX - touch.startX
  const dy = point.clientY - touch.startY
  if (Math.abs(dx) < 56 || Math.abs(dx) < Math.abs(dy) * 1.25) return
  if (dx < 0) goNext()
  else goPrev()
}
function onKeydown(event) {
  if (event.key === 'ArrowLeft') { event.preventDefault(); goPrev() }
  if (event.key === 'ArrowRight') { event.preventDefault(); goNext() }
}

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<style scoped>
.home-carousel { position: relative; padding-bottom: 54px; overflow-x: hidden; outline: none; }
.carousel-viewport { position: relative; overflow: hidden; }
.carousel-track { position: relative; width: 100%; }
/* 非当前页脱离文档流，容器高度只跟随当前页，内容少时不再留大片空白 */
.carousel-page { position: absolute; top: 0; left: 0; width: 100%; padding: 0 34px; visibility: hidden; pointer-events: none; }
.carousel-page.active { position: relative; visibility: visible; pointer-events: auto; }
.carousel-page.active.slide-next { animation: jy-slide-next .34s cubic-bezier(.22, .72, .2, 1); }
.carousel-page.active.slide-prev { animation: jy-slide-prev .34s cubic-bezier(.22, .72, .2, 1); }
@keyframes jy-slide-next { from { opacity: 0; transform: translate3d(38px, 0, 0); } to { opacity: 1; transform: none; } }
@keyframes jy-slide-prev { from { opacity: 0; transform: translate3d(-38px, 0, 0); } to { opacity: 1; transform: none; } }

.page-switcher { position: fixed; bottom: 24px; left: 50%; z-index: 12; display: flex; align-items: center; gap: 4px; padding: 4px; background: rgba(255, 252, 253, .95); border: 1px solid #eedbe0; border-radius: 99px; box-shadow: 0 5px 20px rgba(101, 61, 72, .1); transform: translateX(-50%); backdrop-filter: blur(8px); }
.page-switcher button { display: flex; align-items: center; gap: 6px; padding: 6px 12px; color: #a28c92; font-size: 10.5px; background: transparent; border: 0; border-radius: 99px; cursor: pointer; transition: color .18s, background .18s; white-space: nowrap; }
.page-switcher button i { width: 5px; height: 5px; border-radius: 50%; background: #d9bec4; transition: background .18s; }
.page-switcher button.active { color: #8c4656; background: #fbebef; font-weight: 600; }
.page-switcher button.active i { background: #b86f80; }
.page-switcher button:hover:not(.active) { color: #7d6069; background: #fdf6f8; }

@media (max-width: 900px) {
  .home-carousel { padding-bottom: 70px; }
  .carousel-page { padding: 0 13px; }
  .page-switcher { bottom: 19px; }
}
@media (prefers-reduced-motion: reduce) {
  .carousel-page.active { animation: none; }
}
</style>


