<template>
  <div class="jy-home">
    <section class="hero">
      <div>
        <p class="kicker">静悦工作台 · 静态预览</p>
        <h1>把每日目标放大，把经营节奏看清</h1>
        <p class="sub">接待、成交、客件、件单价四项核心指标先行展示；后续模块先以静态效果呈现，便于确认珠宝门店的视觉气质。</p>
      </div>
      <div class="goal-card">
        <span>今日销售目标</span>
        <strong>¥ 86,000</strong>
        <div class="bar"><i :style="{ width: '62%' }"></i></div>
        <p>已完成 ¥ 53,280 · 进度 62% · 距目标还差 ¥ 32,720</p>
      </div>
    </section>

    <section class="kpi-grid">
      <article v-for="item in kpis" :key="item.label" class="kpi">
        <p>{{ item.label }}</p>
        <h3>{{ item.value }}</h3>
        <small :class="item.tone">{{ item.hint }}</small>
      </article>
    </section>

    <section class="insight">
      <h2>系统提示</h2>
      <div class="insight-list">
        <div v-for="tip in tips" :key="tip.title" class="tip">
          <b>{{ tip.title }}</b>
          <span>{{ tip.desc }}</span>
        </div>
      </div>
    </section>

    <section class="two-col">
      <article class="panel">
        <header>
          <h2>预订单与核销</h2>
          <em>静态</em>
        </header>
        <ul class="rows">
          <li v-for="order in preorders" :key="order.no">
            <div>
              <strong>{{ order.name }}</strong>
              <span>{{ order.item }} · {{ order.status }}</span>
            </div>
            <b>{{ order.amount }}</b>
          </li>
        </ul>
      </article>
      <article class="panel">
        <header>
          <h2>顾客档案</h2>
          <em>静态</em>
        </header>
        <ul class="rows">
          <li v-for="c in customers" :key="c.name">
            <div>
              <strong>{{ c.name }} <i>{{ c.wx }}</i></strong>
              <span>{{ c.level }} · 阴历 {{ c.lunar }} · 阳历 {{ c.solar }}</span>
            </div>
            <b>{{ c.point }}分</b>
          </li>
        </ul>
      </article>
    </section>

    <section class="module-grid">
      <article v-for="mod in modules" :key="mod.title" class="mod">
        <header>
          <h3>{{ mod.title }}</h3>
          <em>{{ mod.tag }}</em>
        </header>
        <p>{{ mod.desc }}</p>
        <ul>
          <li v-for="line in mod.lines" :key="line">{{ line }}</li>
        </ul>
      </article>
    </section>

    <section class="bottom-grid">
      <article class="panel">
        <header>
          <h2>合香珠偏好</h2>
          <em>香气 / 功效标签</em>
        </header>
        <div class="tags">
          <span v-for="t in scentTags" :key="t">{{ t }}</span>
        </div>
        <p class="note">下单后自动写入顾客档案，便于按香气与功效检索客户。</p>
      </article>
      <article class="panel">
        <header>
          <h2>转介绍台账</h2>
          <em>推荐关系锁定</em>
        </header>
        <ul class="rows compact">
          <li v-for="r in referrals" :key="r.from">
            <div>
              <strong>{{ r.from }} → {{ r.to }}</strong>
              <span>{{ r.note }}</span>
            </div>
            <b>{{ r.amount }}</b>
          </li>
        </ul>
      </article>
      <article class="panel">
        <header>
          <h2>待办与目标</h2>
          <em>不自动同步</em>
        </header>
        <ul class="todo">
          <li v-for="t in todos" :key="t">{{ t }}</li>
        </ul>
      </article>
    </section>
  </div>
</template>

<script>
export default {
  name: 'index',
  data() {
    return {
      kpis: [
        { label: '接待批次', value: '36', hint: '客流偏弱，加强老客互动', tone: 'warn' },
        { label: '成交批次', value: '19', hint: '成交率 52.8%', tone: 'ok' },
        { label: '客件数', value: '1.6', hint: '可增加附加销售', tone: 'warn' },
        { label: '件单价', value: '¥ 2,860', hint: '建议主推定制高客单', tone: 'ok' }
      ],
      tips: [
        { title: '日报已生成', desc: '经营、客户、产品三项数据可在周报/十日汇总中继续查看。' },
        { title: '货品确认', desc: '入库、销售、盘点需一对一确认，避免件数与克重混淆。' },
        { title: '会员清分', desc: '每年自动清分不影响等级，金会员保持金会员。' }
      ],
      preorders: [
        { no: '1', name: '林晚棠', item: '18K 和田玉耳坠', status: '定金已收', amount: '¥ 1,200' },
        { no: '2', name: '周予安', item: '南红手串', status: '待取货核销', amount: '¥ 6,800' },
        { no: '3', name: '沈清和', item: '合香珠·安神', status: '全款待归档', amount: '¥ 880' }
      ],
      customers: [
        { name: '林晚棠', wx: '微信1', level: '金会员', lunar: '八月十六', solar: '10-07', point: 5432 },
        { name: '周予安', wx: '微信2', level: '钻石会员', lunar: '三月初三', solar: '03-31', point: 1280 },
        { name: '沈清和', wx: '微信1', level: '普通会员', lunar: '冬月廿一', solar: '12-19', point: 360 }
      ],
      modules: [
        { title: '入库识货', tag: '规划', desc: '语音自动识别货品属性，区分单件入库与按捆入库。', lines: ['商品编号 / 款式 / 材质 / 成色', '供应商、成本、销售金额', '图片视频一并归档'] },
        { title: '出库开单', tag: '规划', desc: '拍摄货品照片后识别售价并填入订单，代理开单自动取代理价。', lines: ['正品销售支持卡券', '退货不自动退券', '赠送出库不影响利润核算'] },
        { title: '代理管理', tag: '规划', desc: '独立代理价与拿货台账，开单识别后按代理价出单。', lines: ['代理档案', '独立价目', '拿货查询'] },
        { title: '意向客户', tag: '规划', desc: '记录兴趣产品、预算与跟进，可转预订单或销售单。', lines: ['未成交仍保留档案', '纳入日报转化率', '持续跟进'] },
        { title: '短视频运营', tag: '规划', desc: '选题脚本、拍摄计划独立待办，客户来源可绑定视频。', lines: ['素材打标签', '播放数据', '引流成交看板'] },
        { title: '库存与盘点', tag: '规划', desc: '商品大库贯穿入库、销售、定金、销退全周期。', lines: ['按年/月/周/日盘点', '款式数量对账', '实时更新'] }
      ],
      scentTags: ['花香', '药香', '果香', '木质香', '沉香味', '复合香', '静心舒缓', '安神', '清新解郁', '助眠'],
      referrals: [
        { from: '林晚棠', to: '何知夏', note: '推荐订单已入账 · 待发放', amount: '¥ 320' },
        { from: '周予安', to: '陈疏影', note: '累计消费统计中', amount: '¥ 1,080' }
      ],
      todos: ['手动选择待办，不自动并入每日任务', '短视频拍摄计划单独成待办', '补录历史订单需填写真实成交日期']
    }
  }
}
</script>

<style scoped lang="scss">
.jy-home {
  min-height: calc(100vh - 84px);
  margin: -12px;
  padding: 24px 24px 40px;
  background:
    radial-gradient(900px 420px at 100% 0, rgba(255, 214, 224, 0.55), transparent 55%),
    linear-gradient(180deg, #fff8f6 0%, #f7ecef 100%);
  color: #4f3b44;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
}
.hero {
  display: grid;
  grid-template-columns: 1.4fr 0.8fr;
  gap: 20px;
  align-items: stretch;
  margin-bottom: 20px;
}
.kicker {
  margin: 0 0 8px;
  color: #c4788a;
  letter-spacing: 3px;
  font-size: 12px;
}
.hero h1 {
  margin: 0 0 10px;
  font-size: 30px;
  font-weight: 650;
  letter-spacing: 0.5px;
}
.sub {
  margin: 0;
  max-width: 640px;
  line-height: 1.8;
  color: #7d6270;
}
.goal-card {
  background: linear-gradient(160deg, #fff, #ffe9ee);
  border: 1px solid rgba(196, 120, 138, 0.16);
  border-radius: 20px;
  padding: 22px 24px;
  box-shadow: 0 16px 40px rgba(168, 98, 116, 0.08);
}
.goal-card span {
  color: #c4788a;
  font-size: 13px;
}
.goal-card strong {
  display: block;
  margin: 8px 0 14px;
  font-size: 36px;
  letter-spacing: 1px;
}
.bar {
  height: 8px;
  border-radius: 99px;
  background: #f3dbe1;
  overflow: hidden;
}
.bar i {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, #e8aeb8, #c4788a);
}
.goal-card p {
  margin: 10px 0 0;
  font-size: 13px;
  color: #846874;
}
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 14px;
  margin-bottom: 18px;
}
.kpi {
  background: #fffdfc;
  border-radius: 18px;
  padding: 18px 18px 16px;
  border: 1px solid rgba(255, 255, 255, 0.8);
  box-shadow: 0 10px 24px rgba(150, 90, 108, 0.06);
}
.kpi p {
  margin: 0;
  color: #9a7b85;
  font-size: 13px;
}
.kpi h3 {
  margin: 8px 0 6px;
  font-size: 28px;
  font-weight: 650;
}
.kpi small {
  font-size: 12px;
}
.warn { color: #c07a3a; }
.ok { color: #7a9a6a; }
.insight {
  margin-bottom: 18px;
}
.insight h2,
.panel header h2,
.mod h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}
.insight-list {
  margin-top: 10px;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}
.tip {
  background: rgba(255, 255, 255, 0.78);
  border-radius: 16px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.tip b { color: #c4788a; font-size: 13px; }
.tip span { font-size: 13px; color: #6f5661; line-height: 1.6; }
.two-col,
.bottom-grid {
  display: grid;
  gap: 14px;
  margin-bottom: 18px;
}
.two-col { grid-template-columns: 1fr 1fr; }
.bottom-grid { grid-template-columns: 1.1fr 1.1fr 0.9fr; }
.panel,
.mod {
  background: #fffdfc;
  border-radius: 18px;
  padding: 16px 18px 14px;
  box-shadow: 0 10px 24px rgba(150, 90, 108, 0.05);
}
.panel header,
.mod header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
em {
  font-style: normal;
  font-size: 11px;
  color: #c4788a;
  background: #f8e6ea;
  padding: 3px 8px;
  border-radius: 99px;
}
.rows {
  list-style: none;
  margin: 0;
  padding: 0;
}
.rows li {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px solid #f3e4e8;
}
.rows li:last-child { border-bottom: 0; }
.rows strong {
  display: block;
  font-size: 14px;
}
.rows i {
  font-style: normal;
  margin-left: 6px;
  color: #c4788a;
  font-size: 12px;
}
.rows span {
  color: #8a6f79;
  font-size: 12px;
}
.rows b { color: #4f3b44; }
.module-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14px;
  margin-bottom: 18px;
}
.mod p {
  margin: 0 0 10px;
  font-size: 13px;
  line-height: 1.7;
  color: #6f5661;
}
.mod ul {
  margin: 0;
  padding-left: 16px;
  color: #846874;
  font-size: 13px;
  line-height: 1.8;
}
.tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.tags span {
  padding: 6px 10px;
  border-radius: 999px;
  background: #fff1f4;
  color: #a36878;
  font-size: 12px;
}
.note {
  margin: 12px 0 0;
  font-size: 12px;
  color: #8a6f79;
}
.todo {
  margin: 0;
  padding-left: 16px;
  color: #6f5661;
  font-size: 13px;
  line-height: 1.9;
}
.compact li { padding: 8px 0; }

@media (max-width: 1100px) {
  .hero, .kpi-grid, .insight-list, .two-col, .module-grid, .bottom-grid {
    grid-template-columns: 1fr 1fr;
  }
}
@media (max-width: 768px) {
  .jy-home { margin: 0; padding: 16px; }
  .hero, .kpi-grid, .insight-list, .two-col, .module-grid, .bottom-grid {
    grid-template-columns: 1fr;
  }
}
</style>
