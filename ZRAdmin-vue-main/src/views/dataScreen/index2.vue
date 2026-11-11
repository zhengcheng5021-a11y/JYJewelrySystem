<template>
  <full-screen-container>
    <starBackground></starBackground>
    <div class="m-data-screen">
      <div class="header">
        <div class="header-bg-title">自定义大屏数据展示</div>
        <div class="date">{{ timeDate }} {{ hourTime }}</div>
      </div>

      <div class="content-grid">
        <!-- 左侧：纵向指标卡片 + 趋势折线图 -->
        <div class="left-panel">
          <div class="kpi-list">
            <div class="kpi-card" v-for="item in dataList" :key="item.title">
              <div class="kpi-icon">
                <img src="@/assets/images/charts/circle-bg.png" class="circle-bg" />
              </div>
              <div class="kpi-content">
                <div class="kpi-title">{{ item.title }}</div>
                <div class="kpi-value">
                  <count-up :start-val="item.start" :loop="true" :end-val="item.value" />
                </div>
                <div class="kpi-meta">
                  <span>环比</span>
                  <span class="kpi-up">
                    16%
                    <el-icon>
                      <CaretTop />
                    </el-icon>
                  </span>
                  <span>同比</span>
                  <span class="kpi-down">
                    -16%
                    <el-icon>
                      <CaretBottom />
                    </el-icon>
                  </span>
                </div>
              </div>
            </div>
          </div>

          <div class="panel left-bottom-panel">
            <line-charts width="100%" height="100%" />
          </div>
        </div>

        <!-- 中间：主视图区，上大图下两张对比图 -->
        <div class="center-panel">
          <div class="panel center-main-panel">
            <overview-chart />
          </div>
          <div class="center-bottom">
            <div class="panel half-panel">
              <bar-chart />
            </div>
            <div class="panel half-panel">
              <chart-component />
            </div>
          </div>
        </div>

        <!-- 右侧：占比环形图 + 仪表盘 -->
        <div class="right-panel">
          <div class="panel right-top-panel">
            <doughnutCharts />
          </div>
          <div class="panel right-bottom-panel">
            <simple-gauge></simple-gauge>
          </div>
        </div>
      </div>
    </div>
  </full-screen-container>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import CountUp from 'vue-countup-v3'
import FullScreenContainer from '@/views/components/dataScreen/full-screen-container.vue'
import BarChart from '@/views/dashboard/BarChart'
import LineCharts from '@/views/components/dataScreen/lineEcharts'
import simpleGauge from '@/views/components/dataScreen/simpleGauge.vue'
import doughnutCharts from '@/views/components/dataScreen/doughnutCharts.vue'
import OverviewChart from '@/views/components/dataScreen/overviewChart.vue'
import starBackground from '@/views/components/starBackground.vue'
import ChartComponent from '@/components/ChartComponent.vue'

const timeDate = ref('')
const hourTime = ref('')
const time1 = ref(null)

const getHour = () => {
  const date = new Date()
  const hour = date.getHours().toString().padStart(2, '0')
  const minute = date.getMinutes().toString().padStart(2, '0')
  const second = date.getSeconds().toString().padStart(2, '0')
  hourTime.value = `${hour}:${minute}:${second}`
  time1.value = setTimeout(() => {
    if (time1.value) clearTimeout(time1.value)
    getHour()
    changeData()
  }, 1000)
}

const getNowTime = () => {
  const date = new Date()
  const year = date.getFullYear()
  const month = date.getMonth() + 1
  const day = date.getDate()
  timeDate.value = `${year}年${month}月${day}日`
  getHour()
}

onMounted(() => {
  getNowTime()
})

onBeforeUnmount(() => {
  if (time1.value) {
    clearTimeout(time1.value)
    time1.value = null
  }
})

const dataList = ref([
  { title: '在线用户', value: 1200, percent: 78.21, start: 0 },
  { title: '今日订单', value: 3200, percent: 65.3, start: 0 },
  { title: '销售额(万)', value: 86, percent: 32.1, start: 0 },
  { title: '访问量', value: 5300, percent: 48.6, start: 0 },
  { title: '消息数', value: 980, percent: 18.21, start: 0 }
])

function changeData() {
  dataList.value.forEach((ele) => {
    ele.start = ele.value
    ele.value = ele.value + Math.round(Math.random() * 10)
  })
}
</script>

<style lang="scss">
@use './index.scss';
</style>

<style scoped lang="scss">
.content-grid {
  // 100% 为 1080，高度 = 1080 - 91(header) - 12(padding-top) - 16(padding-bottom)
  height: calc(100% - 119px);
  padding: 12px 24px 16px;
  box-sizing: border-box;
  display: grid;
  grid-template-columns: 320px minmax(0, 1fr) 320px;
  grid-column-gap: 16px;
}

.panel {
  background: #042750 url('@/assets/images/charts/charts-bg.png') no-repeat;
  background-size: 100% 100%;
  border: 1px solid rgba(0, 228, 255, 0.25);
  box-shadow: 0 0 20px rgba(0, 228, 255, 0.2);
  border-radius: 4px;
  padding: 8px;
  box-sizing: border-box;
}

.left-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.kpi-list {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.kpi-card {
  display: flex;
  align-items: center;
  padding: 8px 10px;
  border-radius: 4px;
  background: linear-gradient(90deg, rgba(0, 228, 255, 0.25), transparent);
}

.kpi-icon {
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 10px;
}

.kpi-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.kpi-title {
  font-size: 16px;
  color: #ffffff;
}

.kpi-value {
  font-size: 26px;
  color: #00e4ff;
}

.kpi-meta {
  font-size: 12px;
  color: #ffffff;
  display: flex;
  align-items: center;
  gap: 4px;
}

.kpi-up {
  color: #00ff9d;
}

.kpi-down {
  color: #ff6b81;
}

.left-bottom-panel {
  height: 230px;
}

.center-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.center-main-panel {
  flex: 1;
  min-height: 360px;
}

.center-bottom {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  grid-column-gap: 12px;
  height: 230px;
}

.half-panel {
  height: 100%;
}

.right-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.right-top-panel {
  flex: 1;
  min-height: 230px;
}

.right-bottom-panel {
  height: 230px;
}
</style>
