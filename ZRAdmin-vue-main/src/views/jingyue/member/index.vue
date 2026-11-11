<template>
  <div class="jy-member">
    <header class="member-hero">
      <div class="hero-copy">
        <span class="eyebrow">JEWELLERY CLIENTELE</span>
        <h1>会员雅集</h1>
        <p>把每一位客人的偏好、消费轨迹与生日关怀收在一起，先照顾重要的人，再经营更多关系。</p>
        <div class="hero-actions">
          <el-button v-hasPermi="['jingyue:member:add']" type="primary" @click="openEditor()"><el-icon><Plus /></el-icon>录入新会员</el-button>
          <el-button v-hasPermi="['jingyue:member:level']" plain @click="openLevels"><el-icon><Medal /></el-icon>等级体系</el-button>
          <el-button v-hasPermi="['jingyue:member:pointrule']" plain @click="openPointRule"><el-icon><Coin /></el-icon>积分规则</el-button>
          <el-button v-hasPermi="['jingyue:member:clearpoints']" plain @click="clearPoints"><el-icon><RefreshLeft /></el-icon>可用积分清零</el-button>
        </div>
      </div>
      <div class="hero-seal">
        <span>MEMBERS</span>
        <strong>{{ number(dashboard.totalMembers) }}</strong>
        <small>位会员在此相遇</small>
        <i class="seal-ring ring-one"></i><i class="seal-ring ring-two"></i>
      </div>
    </header>

    <section class="metric-shell" v-loading="dashboardLoading">
      <button type="button" class="metric-toggle" :aria-expanded="metricsExpanded" @click="metricsExpanded = !metricsExpanded">
        <span class="metric-toggle-main"><i class="metric-pulse"></i><b>会员经营概览</b></span>
        <span class="metric-summary">
          <span><small>会员总量</small><b>{{ number(dashboard.totalMembers) }}</b></span>
          <span><small>本月新增</small><b>{{ number(dashboard.newThisMonth) }}</b></span>
          <span><small>本月活跃</small><b>{{ number(dashboard.activeThisMonth) }}</b></span>
          <span class="amount"><small>本月消费</small><b>¥{{ money(dashboard.monthAmount) }}</b></span>
        </span>
        <span class="metric-toggle-action"><em>{{ metricsExpanded ? '收起' : '展开' }}</em><el-icon><ArrowUp v-if="metricsExpanded" /><ArrowDown v-else /></el-icon></span>
      </button>
    <div v-show="metricsExpanded" class="metric-board">
      <article class="metric-main">
        <div class="metric-head"><span>会员总量</span><el-icon><UserFilled /></el-icon></div>
        <strong>{{ number(dashboard.totalMembers) }}<em> 位</em></strong>
        <p>当前在册且未删除的会员档案</p>
        <div class="metric-foot"><span>本月新增 {{ number(dashboard.newThisMonth) }} 位</span><b>持续积累</b></div>
      </article>
      <article>
        <span class="metric-label">本月新增</span>
        <strong>{{ number(dashboard.newThisMonth) }}<em> 位</em></strong>
        <small>新客建档</small>
        <div class="metric-icon rose"><el-icon><Star /></el-icon></div>
      </article>
      <article>
        <span class="metric-label">本月活跃</span>
        <strong>{{ number(dashboard.activeThisMonth) }}<em> 位</em></strong>
        <small>本月有消费记录</small>
        <div class="metric-icon gold"><el-icon><TrendCharts /></el-icon></div>
      </article>
      <article class="metric-value">
        <span class="metric-label">本月会员消费</span>
        <strong>¥{{ money(dashboard.monthAmount) }}</strong>
        <small>已完成销售单实付合计</small>
        <div class="metric-icon blush"><el-icon><Wallet /></el-icon></div>
      </article>
    </div>
    </section>

    <div class="member-body">
      <aside class="level-rail">
        <div class="rail-head"><div><span>LEVELS</span><h2>会员分层</h2></div><b>{{ levels.length }}</b></div>
        <button type="button" :class="{ active: !query.levelId }" @click="selectLevel(null)">
          <span class="level-mark all"><el-icon><Collection /></el-icon></span>
          <span class="level-copy"><b>全部会员</b><small>完整会员名册</small></span>
          <em>{{ number(dashboard.totalMembers) }}</em>
        </button>
        <button v-for="level in levels" :key="level.id" type="button" :class="{ active: query.levelId === level.id }" @click="selectLevel(level.id)">
          <span class="level-mark" :class="'rank-' + (level.sortNo || 0)"><el-icon><Medal /></el-icon></span>
          <span class="level-copy"><b>{{ level.levelName }}</b><small>{{ rateText(level.discountRate) }} · {{ level.minPoints }} 积分起</small></span>
          <em>{{ number(levelCount(level)) }}</em>
        </button>
        <div class="rail-note"><el-icon><Gem /></el-icon><p>等级按年度积分自动识别。若客人需要特别照顾，也可以在档案中手动指定等级。</p></div>
      </aside>

      <main class="member-main">
        <div class="toolbar">
          <div class="search-box">
            <el-input v-model="query.keyword" clearable placeholder="搜索姓名、会员号或手机号" :prefix-icon="Search" @keyup.enter="search" />
            <el-select v-model="query.memberWechatNo" clearable filterable placeholder="客户加的微信" class="wechat-filter" @change="search">
              <el-option v-for="item in staffWechats" :key="item.id" :label="(item.nickName || item.userName) + ' · ' + item.wechatNo" :value="item.wechatNo" />
            </el-select>
            <el-select v-model="query.followStatus" clearable placeholder="全部跟进状态" @change="search">
              <el-option v-for="item in followOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
            <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
              <el-option label="正常" :value="0" /><el-option label="停用" :value="1" />
            </el-select>
            <el-select v-model="query.sortBy" placeholder="默认排序" class="sort-filter" @change="search">
              <el-option label="最近消费" value="lastConsume" />
              <el-option label="累计消费" value="totalAmount" />
              <el-option label="建档时间" value="createTime" />
            </el-select>
            <el-select v-model="query.sortDirection" class="direction-filter" @change="search">
              <el-option label="倒序" value="desc" />
              <el-option label="正序" value="asc" />
            </el-select>
            <el-button @click="search">查询</el-button>
            <el-button v-hasPermi="['jingyue:member:export']" :loading="exporting" @click="exportMembers"><el-icon><Download /></el-icon>导出</el-button>
          </div>
          <div class="toolbar-note"><i></i><span>{{ currentLevelName }}</span><small>{{ total }} 位会员</small></div>
        </div>

        <div v-loading="loading" class="member-list" :class="{ 'is-expanded': metricsExpanded }">
          <template v-if="rows.length">
            <article v-for="row in rows" :key="row.id" class="member-card" :class="{ muted: row.status === 1 }" @click="openDetail(row)">
              <div class="avatar" :class="'avatar-' + (row.levelId || 0)">{{ initial(row.memberName) }}</div>
              <div class="identity">
                <div class="name-line"><h3>{{ row.memberName }}</h3><span class="level-tag">{{ row.levelName || '未分级' }}</span><span v-if="row.status === 1" class="off-tag">已停用</span></div>
                <p class="identity-copy">
                  <span class="copy-field">{{ row.memberNo }}<el-tooltip content="复制会员编号" placement="top"><button type="button" class="copy-code" aria-label="复制会员编号" @click.stop="copyValue(row.memberNo, '会员编号')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
                  <span v-if="row.phone" class="copy-field">· {{ row.phone }}<el-tooltip content="复制手机号" placement="top"><button type="button" class="copy-code" aria-label="复制手机号" @click.stop="copyValue(row.phone, '手机号')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
                  <span v-if="row.wechatNo" class="wx-line">微信 {{ row.wechatNo }}</span>
                </p>
                <div class="mini-tags"><span>{{ sexText(row.sex) }}</span><span v-if="row.occupation">{{ row.occupation }}</span><span v-if="row.usualStyleName">{{ row.usualStyleName }}</span><span class="create-time">建档 {{ dateTime(row.createTime) }}</span></div>
              </div>
              <div class="member-value">
                <span>累计消费</span><b>¥{{ money(row.totalAmount) }}</b><small>{{ row.orderCount || 0 }} 次成交 · 客单 ¥{{ money(row.avgTicket) }}</small>
              </div>
              <div class="member-points">
                <span>积分账户</span><b>{{ number(row.pointsBalance) }}<em> 可用</em></b><small>累计 {{ number(row.yearPoints) }} 分</small>
              </div>
              <div class="member-follow" :class="'follow-' + (row.followStatus || 0)">
                <div class="follow-head"><i></i><span>{{ row.lastFollowTime ? '最近跟进' : followText(row.followStatus) }}</span></div>
                <small v-if="row.lastFollowTime">{{ row.lastFollowTypeName || '跟进' }} · {{ dateText(row.lastFollowTime) }}</small>
                <p v-if="row.lastFollowContent" class="follow-snippet">{{ row.lastFollowContent }}</p>
                <small v-else>{{ row.lastConsumeTime ? '最近消费 ' + dateText(row.lastConsumeTime) : '暂无消费' }}</small>
              </div>
              <button class="card-arrow" type="button" @click.stop="openDetail(row)"><el-icon><Right /></el-icon></button>
              <div class="preference-strip">
                <span class="preference-label">偏好品类</span>
                <template v-if="row.stylePreferences?.length">
                  <span v-for="item in row.stylePreferences.slice(0, 4)" :key="item.styleItemId" class="preference-chip">
                    {{ item.styleName }} <em>{{ number(item.qty) }}件</em>
                  </span>
                </template>
                <span v-else class="preference-empty">成交后会按款式分类自动汇总</span>
                <div class="card-quick-actions">
                  <button v-hasPermi="['jingyue:workitem:follow']" type="button" @click.stop="openQuickFollow(row)"><el-icon><ChatDotRound /></el-icon>快速跟进</button>
                  <button v-hasPermi="['jingyue:member:edit']" type="button" @click.stop="openEditor(row)"><el-icon><EditPen /></el-icon>编辑</button>
                </div>
              </div>
            </article>
          </template>
          <div v-else class="empty-member">
            <div class="empty-icon"><el-icon><UserFilled /></el-icon></div>
            <h3>还没有符合条件的会员</h3>
            <p>录入第一位会员，开始记录她的偏好、生日与每一次到店。</p>
            <el-button v-hasPermi="['jingyue:member:add']" type="primary" @click="openEditor()">录入新会员</el-button>
          </div>
        </div>
        <div class="list-foot">
          <span>会员数据仅用于门店服务与经营分析</span>
          <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" />
        </div>
      </main>
    </div>
    <el-drawer v-model="detailOpen" size="640px" append-to-body :with-header="false" class="member-drawer">
      <div v-loading="detailLoading" class="detail-wrap">
        <template v-if="detail.id">
          <div class="drawer-heading"><span>CLIENT PORTRAIT</span><b>会员画像</b><button type="button" @click="detailOpen = false"><el-icon><Close /></el-icon></button></div>
          <section class="portrait">
            <div class="portrait-avatar">{{ initial(detail.memberName) }}</div>
            <div class="portrait-main">
              <div class="portrait-title"><h2>{{ detail.memberName }}</h2><span>{{ detail.levelName || '未分级' }}</span></div>
              <p class="identity-copy">
                <span class="copy-field">{{ detail.memberNo }}<el-tooltip content="复制会员编号" placement="top"><button type="button" class="copy-code" aria-label="复制会员编号" @click.stop="copyValue(detail.memberNo, '会员编号')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
                <span v-if="detail.phone" class="copy-field">· {{ detail.phone }}<el-tooltip content="复制手机号" placement="top"><button type="button" class="copy-code" aria-label="复制手机号" @click.stop="copyValue(detail.phone, '手机号')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
              </p>
              <div class="portrait-tags"><span>{{ sexText(detail.sex) }}</span><span>{{ rateText(detail.discountRate) }}</span><span>{{ followText(detail.followStatus) }}</span></div>
            </div>
            <el-dropdown trigger="click">
              <button class="more-btn" type="button"><el-icon><MoreFilled /></el-icon></button>
              <template #dropdown><el-dropdown-menu>
                <el-dropdown-item v-hasPermi="['jingyue:member:edit']" @click="openEditor(detail)">编辑档案</el-dropdown-item>
                <el-dropdown-item v-hasPermi="['jingyue:member:points']" @click="openPoints(detail)">调整积分</el-dropdown-item>
                <el-dropdown-item v-hasPermi="['jingyue:member:remove']" divided @click="removeMember(detail)">删除会员</el-dropdown-item>
              </el-dropdown-menu></template>
            </el-dropdown>
          </section>

          <section class="portrait-stats">
            <div><span>累计消费</span><strong>¥{{ money(detail.totalAmount) }}</strong></div>
            <div><span>成交次数</span><strong>{{ detail.orderCount || 0 }}<em> 次</em></strong></div>
            <div><span>累计积分</span><strong>{{ number(detail.yearPoints) }}</strong><em> 历史累计</em></div>
            <div><span>可用积分</span><strong>{{ number(detail.pointsBalance) }}</strong><em> 当前可用</em></div>
            <div><span>客单价</span><strong>¥{{ money(detail.avgTicket) }}</strong></div>
          </section>

          <section class="detail-section">
            <div class="section-caption with-action">
              <div><span>CLIENT PREFERENCE</span><h3>偏好品类</h3></div>
              <el-button v-hasPermi="['jingyue:member:edit']" link type="primary" @click="openEditor(detail)">编辑常购款式</el-button>
            </div>
            <div v-if="detail.stylePreferences?.length" class="preference-panel">
              <article v-for="item in detail.stylePreferences" :key="item.styleItemId">
                <div class="preference-name"><b>{{ item.styleName }}</b><small>{{ item.orderCount }} 单 · {{ number(item.qty) }} 件</small></div>
                <div class="preference-track"><i :style="{ width: preferencePercent(item) + '%' }"></i></div>
                <strong>¥{{ money(item.amount) }}</strong>
              </article>
            </div>
            <div v-else class="soft-note"><span>暂无消费</span><p>客户完成销售单后，系统会按一级款式分类自动汇总购买金额和件数。</p></div>
          </section>

          <section class="detail-section">
            <div class="section-caption with-action">
              <div><span>FOLLOW UP</span><h3>最近跟进</h3></div>
              <el-button v-hasPermi="['jingyue:workitem:follow']" type="primary" plain size="small" :icon="ChatDotRound" @click="openQuickFollow(detail)">新增跟进</el-button>
            </div>
            <div class="follow-summary">
              <template v-if="detail.lastFollowTime">
                <div class="follow-badge">{{ detail.lastFollowTypeName || '客户跟进' }}</div>
                <div class="follow-content"><b>{{ dateTime(detail.lastFollowTime) }}</b><p>{{ detail.lastFollowContent || '本次跟进未填写内容' }}</p><small>累计 {{ detail.followCount || 0 }} 次跟进</small></div>
              </template>
              <div v-else class="points-empty"><el-icon><ChatDotRound /></el-icon><p>还没有跟进记录，先记录一次沟通。</p></div>
            </div>
          </section>

          <section class="detail-section">
            <div class="section-caption"><span>PROFILE</span><h3>偏好与纪念日</h3></div>
            <div class="info-grid">
              <div><span>生日</span><b>{{ birthdayText(detail) || '未记录' }}</b></div>
              <div><span>本人微信</span><b>{{ detail.wechatNo || '未记录' }}</b></div>
              <div><span>客户加的微信</span><b>{{ detail.memberWechatNo ? detail.memberWechatNo + (detail.memberWechatStaffName ? ' · ' + detail.memberWechatStaffName : '') : '未记录' }}</b></div>
              <div><span>推荐人</span><b>{{ referrerSummary(detail) }}</b></div>
              <div><span>职业</span><b>{{ detail.occupation || '未记录' }}</b></div>
              <div><span>常购款式</span><b>{{ detail.usualStyleName || '未记录' }}</b></div>
              <div><span>最近消费</span><b>{{ detail.lastConsumeTime ? dateTime(detail.lastConsumeTime) : '暂无消费' }}</b></div>
              <div><span>建档时间</span><b>{{ detail.createTime ? dateTime(detail.createTime) : '未记录' }}</b></div>
              <div><span>五行偏好</span><b>{{ detail.wuXing || '未记录' }}</b></div>
              <div><span>推荐方向</span><b>{{ detail.recommendDirection || '未记录' }}</b></div>
            </div>
            <div class="soft-note"><span>爱好</span><p>{{ detail.hobby || '尚未记录客人的兴趣与偏好，可在编辑档案中补充。' }}</p></div>
            <div v-if="detail.remark" class="soft-note"><span>服务备注</span><p>{{ detail.remark }}</p></div>
          </section>

          <section class="detail-section">
            <div class="section-caption with-action">
              <div><span>REFERRAL LINEAGE</span><h3>推荐关系族谱</h3></div>
              <el-button link type="primary" :loading="graphLoading" @click="loadReferralGraph(detail.id)">刷新关系</el-button>
            </div>
            <div v-loading="graphLoading" class="lineage-panel">
              <template v-if="referralGraph?.focus">
                <div class="mindmap-toolbar">
                  <span><i class="dot up"></i>上级推荐人</span>
                  <span><i class="dot self"></i>当前客户</span>
                  <span><i class="dot down"></i>下级客户</span>
                  <small>最多追溯上下三级，点击节点可切换查看</small>
                </div>
                <div class="mindmap-scroll">
                  <div class="mindmap">
                    <div class="mindmap-side up-side">
                      <div v-for="level in upLevels" :key="'u' + level.depth" class="mindmap-level up-level">
                        <div class="mindmap-level-label">上级 {{ level.depth }} 级</div>
                        <div class="mindmap-nodes">
                          <button v-for="node in level.nodes" :key="node.memberId" type="button" class="mind-node up" @click="switchGraphFocus(node.memberId)">
                            <span class="mind-node-top"><em>L{{ level.depth }}</em><i>{{ node.memberName }}</i></span>
                            <small>{{ node.wechatNo || node.phone || '无联系方式' }}</small>
                            <span class="mind-node-foot">{{ node.memberNo }} · {{ node.levelName || '未分级' }}</span>
                          </button>
                        </div>
                      </div>
                      <div v-if="!referralGraph.upChain?.length" class="mindmap-empty up-empty">暂无上级推荐人</div>
                    </div>
                    <div class="mindmap-side focus-side">
                      <div class="mindmap-level-label focus-label">当前客户</div>
                      <article class="mind-node focus">
                        <span class="mind-node-top"><em>本人</em><i>{{ referralGraph.focus.memberName }}</i></span>
                        <small>{{ referralGraph.focus.wechatNo || referralGraph.focus.phone || '无联系方式' }}</small>
                        <span class="mind-node-foot">{{ referralGraph.focus.memberNo }} · {{ referralGraph.focus.levelName || '未分级' }}</span>
                        <span class="mind-focus-stats">累计 ¥{{ money(referralGraph.focus.totalAmount) }} · 奖励 ¥{{ money(referralGraph.focus.rewardTotal) }}</span>
                      </article>
                    </div>
                    <div class="mindmap-side down-side">
                      <div v-for="level in downLevels" :key="'d' + level.depth" class="mindmap-level down-level">
                        <div class="mindmap-level-label">下级 {{ level.depth }} 级</div>
                        <div class="mindmap-nodes">
                          <button v-for="node in level.nodes" :key="node.memberId" type="button" class="mind-node down" @click="switchGraphFocus(node.memberId)">
                            <span class="mind-node-top"><em>L{{ level.depth }}</em><i>{{ node.memberName }}</i></span>
                            <small>{{ node.wechatNo || node.phone || '无联系方式' }}</small>
                            <span class="mind-node-foot">{{ node.memberNo }} · {{ node.levelName || '未分级' }}</span>
                          </button>
                        </div>
                      </div>
                      <div v-if="!referralGraph.downList?.length" class="mindmap-empty down-empty">暂无下级推荐客户</div>
                    </div>
                  </div>
                </div>
              </template>
              <div v-else class="lineage-empty large">该客户暂未形成推荐关系。</div>
            </div>
          </section>

          <section class="detail-section">
            <div class="section-caption with-action"><div><span>PURCHASE HISTORY</span><h3>消费记录</h3></div><span class="history-count">{{ historySales.length }} 单</span></div>
            <div v-loading="historyLoading" class="history-list">
              <article v-for="item in historySales" :key="item.id" class="history-row">
                <span class="history-dot" :class="item.status === 1 ? 'done' : 'draft'"></span>
                <div class="history-copy"><b>{{ item.orderNo }}</b><small>{{ dateTime(item.dealTime) }} · {{ number(item.pieceQty) }} 件 · {{ item.status === 1 ? '已完成' : item.status === 9 ? '作废' : '草稿' }}</small></div>
                <strong>¥{{ money(item.payAmount) }}</strong>
              </article>
              <div v-if="!historySales.length && !historyLoading" class="points-empty"><el-icon><Coin /></el-icon><p>还没有消费记录</p></div>
            </div>
          </section>
          <section class="detail-section ledger-section">
            <div class="section-caption with-action"><div><span>POINTS LEDGER</span><h3>积分流水</h3></div><el-button v-hasPermi="['jingyue:member:points']" link type="primary" @click="openPoints(detail)">调整积分</el-button></div>
            <div v-loading="pointsLoading" class="point-list">
              <article v-for="item in pointRows" :key="item.id" class="point-row">
                <i :class="item.changePoints >= 0 ? 'up' : 'down'"></i>
                <div><b>{{ item.reason || pointTypeText(item.changeType) }}</b><small>{{ dateTime(item.bizTime) }} · 余额 {{ number(item.balanceAfter) }}</small></div>
                <strong :class="item.changePoints >= 0 ? 'up' : 'down'">{{ item.changePoints >= 0 ? '+' : '' }}{{ number(item.changePoints) }}</strong>
              </article>
              <div v-if="!pointRows.length && !pointsLoading" class="points-empty"><el-icon><Coin /></el-icon><p>还没有积分变动记录</p></div>
            </div>
          </section>
        </template>
      </div>
    </el-drawer>
    <el-drawer v-model="editorOpen" size="620px" append-to-body :with-header="false">
      <div class="editor-wrap">
        <div class="drawer-heading"><span>{{ form.id ? 'EDIT PROFILE' : 'NEW CLIENT' }}</span><b>{{ form.id ? '编辑会员档案' : '录入新会员' }}</b><button type="button" @click="editorOpen = false"><el-icon><Close /></el-icon></button></div>
        <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="member-form">
          <div class="form-section"><div class="form-caption"><span>01</span><b>基本资料</b></div>
            <div class="form-grid two">
              <el-form-item label="会员姓名" prop="memberName"><el-input v-model="form.memberName" maxlength="50" placeholder="请输入姓名" /></el-form-item>
              <el-form-item label="手机号"><el-input v-model="form.phone" maxlength="30" placeholder="选填" /></el-form-item>
              <el-form-item label="本人微信号"><el-input v-model="form.wechatNo" maxlength="64" placeholder="客户本人微信，任意搜索都能命中" /></el-form-item>
              <el-form-item label="客户加的微信" class="member-wechat-field">
                <el-select v-model="form.memberWechatId" clearable filterable placeholder="选择员工微信（可不选）" no-data-text="请先在用户管理中维护员工微信">
                  <el-option v-for="item in staffWechats" :key="item.id" :label="(item.nickName || item.userName) + ' · ' + item.wechatNo + (item.isPrimary ? '（默认）' : '')" :value="item.id" />
                </el-select>
                <p class="field-hint">数据来自用户管理，只能选择不能手填；可留空。</p>
              </el-form-item>
              <el-form-item label="推荐人" class="referrer-form-item">
                <div class="referrer-editor">
                  <div v-for="(item, index) in form.referrers" :key="item.key" class="referrer-row">
                    <el-input v-model="item.referrerWechatNo" maxlength="64" placeholder="推荐人微信号" :disabled="item.original" />
                    <el-radio v-model="primaryReferrerKey" :value="item.key" class="primary-radio">默认</el-radio>
                    <el-button v-if="!item.original || form.referrers.length > 1" link type="danger" :icon="Delete" title="移出推荐关系" @click="removeReferrerRow(index)" />
                  </div>
                  <el-button link type="primary" :icon="Plus" @click="addReferrerRow">添加推荐人</el-button>
                  <p class="field-hint">同一客户可有多个推荐人；销售开单时只选择本次成交的一位。已有返现的关系不可物理删除。</p>
                </div>
              </el-form-item>
              <el-form-item label="性别"><el-radio-group v-model="form.sex"><el-radio :value="0">保密</el-radio><el-radio :value="1">女</el-radio><el-radio :value="2">男</el-radio></el-radio-group></el-form-item>
              <el-form-item label="会员等级"><el-select v-model="form.levelId" clearable placeholder="系统按积分自动匹配"><el-option v-for="level in levels" :key="level.id" :label="level.levelName + ' · ' + rateText(level.discountRate)" :value="level.id" /></el-select></el-form-item>
            </div>
          </div>
          <div class="form-section"><div class="form-caption"><span>02</span><b>偏好与跟进</b></div>
            <div class="form-grid two">
              <el-form-item label="生日历法">
                <el-radio-group v-model="form.birthCalendar" @change="changeBirthCalendar">
                  <el-radio-button :value="1">阳历</el-radio-button>
                  <el-radio-button :value="2">阴历</el-radio-button>
                </el-radio-group>
              </el-form-item>
              <el-form-item :label="form.birthCalendar === 2 ? '农历生日' : '公历生日'">
                <el-date-picker v-if="form.birthCalendar === 1" v-model="form.solarBirthday" type="date" value-format="YYYY-MM-DD" placeholder="选择公历生日" style="width: 100%" />
                <el-input v-else v-model="form.lunarBirthday" maxlength="30" placeholder="例如：八月十五、腊月初八" />
              </el-form-item>
              <el-form-item label="出生年月">
                <el-input-number v-model="form.birthYear" :min="1900" :max="currentYear" controls-position="right" placeholder="出生年份" style="width: 100%" />
              </el-form-item>
              <el-form-item label="职业"><el-input v-model="form.occupation" maxlength="50" placeholder="选填" /></el-form-item>
              <el-form-item label="常购款式"><el-select v-model="form.usualStyleItemId" clearable filterable placeholder="选择款式分类"><el-option v-for="item in categories" :key="item.id" :label="categoryLabel(item)" :value="item.id" /></el-select></el-form-item>
              <el-form-item label="跟进状态"><el-select v-model="form.followStatus"><el-option v-for="item in followOptions" :key="item.value" :label="item.label" :value="item.value" /></el-select></el-form-item>
              <el-form-item label="档案状态"><el-select v-model="form.status"><el-option label="正常" :value="0" /><el-option label="停用" :value="1" /></el-select></el-form-item>
              <el-form-item label="五行偏好"><el-input v-model="form.wuXing" maxlength="20" placeholder="选填" /></el-form-item>
              <el-form-item label="推荐方向"><el-input v-model="form.recommendDirection" maxlength="100" placeholder="如：手串、项链、银饰" /></el-form-item>
            </div>
            <el-form-item label="爱好"><el-input v-model="form.hobby" type="textarea" :rows="2" maxlength="500" show-word-limit placeholder="记录客人的风格偏好、材质喜好、送礼场景等" /></el-form-item>
          </div>
          <div class="form-section"><div class="form-caption"><span>03</span><b>服务备注</b></div>
            <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="仅内部可见的服务提醒" /></el-form-item>
          </div>
        </el-form>
        <div class="editor-actions"><el-button @click="editorOpen = false">取消</el-button><el-button type="primary" :loading="saving" @click="submit">保存档案</el-button></div>
      </div>
    </el-drawer>

    <el-dialog v-model="pointsOpen" title="调整会员积分" width="460px" append-to-body>
      <div class="dialog-intro"><span>{{ pointsMember.memberName }}</span><small>可用 {{ number(pointsMember.pointsBalance) }} · 累计 {{ number(pointsMember.yearPoints) }}</small></div>
      <el-form label-position="top">
        <el-form-item label="调整可用积分"><el-input-number v-model="pointsForm.changePoints" :precision="0" :step="100" controls-position="right" style="width: 100%" /><p class="field-hint">正数增加，负数扣减；调整后可用余额不能为负数，累计积分会随积分增加、扣减及退货同步变化。</p></el-form-item>
        <el-form-item label="调整原因"><el-input v-model="pointsForm.reason" type="textarea" :rows="3" maxlength="200" show-word-limit placeholder="例如：生日关怀赠分、活动补偿" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="pointsOpen = false">取消</el-button><el-button type="primary" :loading="pointsSaving" @click="submitPoints">确认调整</el-button></template>
    </el-dialog>

    <el-dialog v-model="followOpen" title="快速记录跟进" width="560px" append-to-body destroy-on-close class="quick-follow-dialog" @closed="resetFollow">
      <div class="follow-target" v-if="followMember.id">
        <div class="follow-target-avatar">{{ initial(followMember.memberName) }}</div>
        <div><b>{{ followMember.memberName }}</b><p>{{ followMember.phone || '未留手机号' }} · {{ followMember.levelName || '未分级' }}</p></div>
        <span>{{ number(followMember.pointsBalance) }} 积分</span>
      </div>
      <el-form ref="followFormRef" :model="followForm" :rules="followRules" label-position="top">
        <div class="follow-form-grid">
          <el-form-item label="跟进方式" prop="lookupItemId">
            <el-select v-model="followForm.lookupItemId" filterable style="width: 100%" placeholder="选择跟进方式">
              <el-option v-for="item in followWays" :key="item.id" :label="item.itemName" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="下次跟进">
            <el-date-picker v-model="followForm.nextFollowTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss" placeholder="可选" style="width: 100%" />
          </el-form-item>
        </div>
        <el-form-item label="跟进内容" prop="content">
          <el-input v-model="followForm.content" type="textarea" :rows="4" maxlength="1000" show-word-limit placeholder="客户关注哪些款式、预算、佩戴场景或顾虑？" />
        </el-form-item>
        <el-form-item label="跟进结果">
          <el-input v-model="followForm.result" maxlength="500" placeholder="例如：已约周六到店试戴手串" />
        </el-form-item>
        <div class="follow-tip"><el-icon><Clock /></el-icon><span>跟进时间由服务器自动记录；跟进方式在「数据字典 - 跟进方式」中维护。</span></div>
      </el-form>
      <template #footer><el-button @click="followOpen = false">取消</el-button><el-button type="primary" :loading="followSaving" @click="submitFollow">保存跟进</el-button></template>
    </el-dialog>

    <el-dialog v-model="pointRuleOpen" title="积分规则" width="470px" append-to-body>
      <div class="dialog-intro"><span>消费金额兑换积分</span><small>保存后立即用于销售结算</small></div>
      <el-form label-position="top">
        <div class="form-grid two">
          <el-form-item label="每消费金额（元）"><el-input-number v-model="pointRule.amountBase" :min="0.01" :precision="2" controls-position="right" style="width: 100%" /></el-form-item>
          <el-form-item label="奖励积分"><el-input-number v-model="pointRule.pointsAwarded" :min="0" :precision="2" controls-position="right" style="width: 100%" /></el-form-item>
        </div>
        <div class="cash-rule-block">
          <div class="cash-rule-head"><span>转介绍返现</span><small>推荐人奖励口径，0 表示不启用</small></div>
          <div class="form-grid two">
            <el-form-item label="返现比例（%）"><el-input-number v-model="pointRule.referralCashRate" :min="0" :max="100" :precision="2" controls-position="right" style="width: 100%" /></el-form-item>
            <el-form-item label="阶梯基数（元）"><el-input-number v-model="pointRule.cashBase" :min="0" :precision="2" controls-position="right" style="width: 100%" /></el-form-item>
            <el-form-item label="每次返现（元）"><el-input-number v-model="pointRule.cashAwarded" :min="0" :precision="2" controls-position="right" style="width: 100%" /></el-form-item>
          </div>
          <p class="field-hint">返现比例大于 0 时按「实付金额 × 比例」结算；为 0 时按「实付 ÷ 阶梯基数 取整 × 每次返现」结算。两者同时为 0 则不产生推荐奖励。</p>
        </div>
        <el-form-item label="规则说明"><el-input v-model="pointRule.remark" type="textarea" :rows="2" maxlength="200" show-word-limit placeholder="例如：每消费1元累计1积分" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="pointRule.status" :active-value="0" :inactive-value="1" active-text="启用" inactive-text="停用" /></el-form-item>
      </el-form>
      <p class="field-hint">示例：每消费 {{ money(pointRule.amountBase) }} 元获得 {{ number(pointRule.pointsAwarded) }} 积分，按实付金额向下取整。</p>
      <template #footer><el-button @click="pointRuleOpen = false">取消</el-button><el-button type="primary" :loading="pointRuleSaving" @click="submitPointRule">保存规则</el-button></template>
    </el-dialog>
    <el-dialog v-model="levelsOpen" title="会员等级体系" width="820px" append-to-body class="level-dialog">
      <p class="dialog-lead">等级按年度积分自动匹配，折扣率用于销售结算。调整积分区间时请避免重叠。</p>
      <div class="level-table">
        <div class="level-table-head"><span>等级</span><span>积分区间</span><span>折扣</span><span>年度清零</span><span>状态</span><span></span></div>
        <div v-for="level in levelRows" :key="level.id" class="level-table-row">
          <span class="level-name"><el-icon><Medal /></el-icon>{{ level.levelName }}<small>{{ level.levelCode }}</small></span>
          <span>{{ number(level.minPoints) }} - {{ level.maxPoints == null ? '不限' : number(level.maxPoints) }}</span>
          <span>{{ rateText(level.discountRate) }}</span>
          <span>{{ level.keepOnYearClear === 1 ? '是' : '否' }}</span>
          <span :class="level.status === 0 ? 'on' : 'off'">{{ level.status === 0 ? '启用' : '停用' }}</span>
          <el-button v-hasPermi="['jingyue:member:level']" link type="primary" @click="editLevel(level)">编辑</el-button>
        </div>
      </div>
      <template #footer><el-button @click="levelsOpen = false">关闭</el-button><el-button v-hasPermi="['jingyue:member:level']" type="primary" @click="editLevel()">新增等级</el-button></template>
    </el-dialog>

    <el-dialog v-model="levelEditOpen" :title="levelForm.id ? '编辑会员等级' : '新增会员等级'" width="520px" append-to-body>
      <el-form ref="levelFormRef" :model="levelForm" :rules="levelRules" label-position="top">
        <div class="form-grid two">
          <el-form-item label="等级编码" prop="levelCode"><el-input v-model="levelForm.levelCode" maxlength="32" placeholder="如 vip_gold" /></el-form-item>
          <el-form-item label="等级名称" prop="levelName"><el-input v-model="levelForm.levelName" maxlength="50" placeholder="如 金卡会员" /></el-form-item>
          <el-form-item label="起始积分"><el-input-number v-model="levelForm.minPoints" :min="0" controls-position="right" style="width: 100%" /></el-form-item>
          <el-form-item label="结束积分"><el-input-number v-model="levelForm.maxPoints" :min="0" controls-position="right" placeholder="留空表示不限" style="width: 100%" /></el-form-item>
          <el-form-item label="折扣率"><el-input-number v-model="levelForm.discountRate" :min="0.01" :max="1" :step="0.01" :precision="2" controls-position="right" style="width: 100%" /><p class="field-hint">0.95 表示 95 折</p></el-form-item>
          <el-form-item label="排序号"><el-input-number v-model="levelForm.sortNo" :min="0" controls-position="right" style="width: 100%" /></el-form-item>
          <el-form-item label="年度清零"><el-switch v-model="levelForm.keepOnYearClear" :active-value="1" :inactive-value="0" active-text="是" inactive-text="否" /></el-form-item>
          <el-form-item label="状态"><el-switch v-model="levelForm.status" :active-value="0" :inactive-value="1" active-text="启用" inactive-text="停用" /></el-form-item>
        </div>
        <el-form-item label="备注"><el-input v-model="levelForm.remark" type="textarea" :rows="2" maxlength="500" show-word-limit /></el-form-item>
      </el-form>
      <template #footer><el-button @click="levelEditOpen = false">取消</el-button><el-button type="primary" :loading="levelSaving" @click="submitLevel">保存等级</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyMember">
import { Search, Plus, Medal, Star, TrendCharts, Wallet, Collection, Right, Close, MoreFilled, Coin, UserFilled, Opportunity as Gem, ChatDotRound, EditPen, Clock, Delete, Download, CopyDocument, ArrowDown, ArrowUp } from '@element-plus/icons-vue'
import { useClipboard } from '@vueuse/core'
import { listMember, exportMember, getMember, getMemberOptions, getMemberDashboard, saveMember, deleteMember, listMemberLevels, saveMemberLevel, adjustMemberPoints, listMemberPoints, getMemberHistory, getPointRule, savePointRule, clearAllPoints } from '@/api/jingyue/member'
import { lookupOptions } from '@/api/jingyue/lookup'
import { saveMemberFollow } from '@/api/jingyue/workitem'
import { getReferralGraph } from '@/api/jingyue/referral'
import { serverNow } from '@/utils/serverTime'

const { proxy } = getCurrentInstance()
const { copy, isSupported } = useClipboard({ legacy: true })
const loading = ref(false)
const exporting = ref(false)
const dashboardLoading = ref(false)
const metricsExpanded = ref(false)
const detailLoading = ref(false)
const pointsLoading = ref(false)
const saving = ref(false)
const pointsSaving = ref(false)
const levelSaving = ref(false)
const followOpen = ref(false)
const followSaving = ref(false)
const followFormRef = ref()
const followMember = reactive({})
const followWays = ref([])
const followForm = reactive({ lookupItemId: undefined, content: '', result: '', nextFollowTime: '' })
const rows = ref([])
const total = ref(0)
const levels = ref([])
const categories = ref([])
const staffWechats = ref([])
const pointRows = ref([])
const historyLoading = ref(false)
const historySales = ref([])
const graphLoading = ref(false)
const referralGraph = ref(null)
const pointRuleOpen = ref(false)
const pointRuleSaving = ref(false)
const pointRule = reactive({ id: 0, amountBase: 1, pointsAwarded: 1, cashBase: 0, cashAwarded: 0, referralCashRate: 0, status: 0, remark: '' })
const dashboard = reactive({ totalMembers: 0, newThisMonth: 0, activeThisMonth: 0, monthAmount: 0, levels: [] })
const query = reactive({ pageNum: 1, pageSize: 10, keyword: '', memberWechatNo: undefined, status: 0, levelId: undefined, followStatus: undefined, sortBy: 'lastConsume', sortDirection: 'desc' })
const detailOpen = ref(false)
const detail = reactive({})
const editorOpen = ref(false)
const formRef = ref()
const form = reactive(emptyForm())
const primaryReferrerKey = ref(0)
const pointsOpen = ref(false)
const pointsMember = reactive({})
const pointsForm = reactive({ memberId: 0, changePoints: 0, reason: '' })
const levelsOpen = ref(false)
const levelRows = ref([])
const levelEditOpen = ref(false)
const levelFormRef = ref()
const levelForm = reactive(emptyLevel())
const currentYear = serverNow().getFullYear()
const followOptions = [{ value: 0, label: '待跟进' }, { value: 1, label: '跟进中' }, { value: 2, label: '暂缓' }, { value: 3, label: '已完成' }]
const rules = { memberName: [{ required: true, message: '请输入会员姓名', trigger: 'blur' }] }
const followRules = {
  lookupItemId: [{ required: true, message: '请选择跟进方式', trigger: 'change' }],
  content: [{ required: true, message: '请输入跟进内容', trigger: 'blur' }]
}
const levelRules = {
  levelCode: [{ required: true, message: '请输入等级编码', trigger: 'blur' }],
  levelName: [{ required: true, message: '请输入等级名称', trigger: 'blur' }]
}
const currentLevelName = computed(() => query.levelId ? (levels.value.find((it) => it.id === query.levelId)?.levelName || '指定等级') : '全部会员')
const upLevels = computed(() => groupGraphLevels(referralGraph.value?.upChain))
const downLevels = computed(() => groupGraphLevels(referralGraph.value?.downList))

function groupGraphLevels(list) {
  const grouped = new Map()
  for (const node of list || []) {
    const depth = Math.abs(Number(node.depth) || 1)
    if (!grouped.has(depth)) grouped.set(depth, [])
    grouped.get(depth).push(node)
  }
  return [1, 2, 3].filter((depth) => grouped.has(depth)).map((depth) => ({ depth, nodes: grouped.get(depth) }))
}

function emptyForm() {
  return { id: 0, memberName: '', phone: '', wechatNo: '', memberWechatId: undefined, sex: 0, solarBirthday: '', lunarBirthday: '', birthCalendar: 1, birthYear: undefined, occupation: '', hobby: '', wuXing: '', recommendDirection: '', levelId: undefined, followStatus: 0, usualStyleItemId: undefined, status: 0, remark: '', referrers: [] }
}
function emptyLevel() {
  return { id: 0, levelCode: '', levelName: '', minPoints: 0, maxPoints: undefined, discountRate: 1, sortNo: 0, keepOnYearClear: 1, status: 0, remark: '' }
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function initial(name) { return (name || '静').trim().slice(0, 1) }
async function copyValue(value, label) {
  const text = String(value || '').trim()
  if (!text) return proxy.$modal.msgWarning(`没有可复制的${label}`)
  if (!isSupported.value) return proxy.$modal.msgWarning('当前浏览器不支持自动复制')
  await copy(text)
  proxy.$modal.msgSuccess(`${label}已复制`)
}
function rateText(value) { const rate = Number(value || 1); return rate >= 1 ? '无折扣' : (rate * 10).toFixed(1).replace(/\.0$/, '') + ' 折' }
function sexText(value) { return value === 1 ? '女士' : value === 2 ? '先生' : '未标注' }
function followText(value) { return followOptions.find((it) => it.value === value)?.label || '待跟进' }
function pointTypeText(value) { return value === 1 ? '消费获得' : value === 2 ? '积分抵扣' : value === 3 ? '手工调整' : '积分变动' }
function categoryLabel(item) { return item.parentId ? '　└ ' + item.itemName : item.itemName }
function dateText(value) { return value ? String(value).slice(0, 10) : '' }
function dateTime(value) { if (!value) return ''; return String(value).replace('T', ' ').slice(0, 16) }
function birthdayText(row) {
  const birthday = row.solarBirthday ? dateText(row.solarBirthday) + '（公历）' : row.lunarBirthday ? row.lunarBirthday + '（农历）' : ''
  return birthday && row.birthYear ? birthday + ' · ' + row.birthYear + ' 年' : birthday
}
function changeBirthCalendar(value) {
  if (value === 1) form.lunarBirthday = ''
  else form.solarBirthday = ''
}
function levelCount(level) { const stat = (dashboard.levels || []).find((it) => it.levelName === level.levelName); return stat?.count || 0 }
function preferencePercent(item) {
  const max = Math.max(...(detail.stylePreferences || []).map((it) => Number(it.amount) || 0), 1)
  return Math.max(4, Math.round((Number(item.amount || 0) / max) * 100))
}
function loadList() {
  loading.value = true
  listMember(query).then((res) => { rows.value = res.data?.result || []; total.value = res.data?.totalNum || 0 }).finally(() => { loading.value = false })
}
function loadDashboard() {
  dashboardLoading.value = true
  getMemberDashboard().then((res) => Object.assign(dashboard, res.data || {})).finally(() => { dashboardLoading.value = false })
}
function loadOptions() {
  const optionsTask = getMemberOptions().then((res) => { levels.value = res.data?.levels || []; categories.value = res.data?.categories || []; staffWechats.value = res.data?.staffWechats || [] })
  const followTask = lookupOptions('follow_way').then((res) => { followWays.value = res.data || [] }).catch(() => { followWays.value = [] })
  return Promise.all([optionsTask, followTask])
}
function refreshAll() { loadOptions(); loadDashboard(); loadList() }
function search() { query.pageNum = 1; loadList() }
async function exportMembers() {
  if (exporting.value) return
  exporting.value = true
  try {
    await exportMember({ keyword: query.keyword, memberWechatNo: query.memberWechatNo, status: query.status, levelId: query.levelId, followStatus: query.followStatus, sortBy: query.sortBy, sortDirection: query.sortDirection })
  } finally {
    exporting.value = false
  }
}
function selectLevel(id) { query.levelId = id || undefined; query.pageNum = 1; loadList() }
function openDetail(row) {
  detailOpen.value = true
  detailLoading.value = true
  getMember(row.id).then((res) => {
    Object.keys(detail).forEach((key) => delete detail[key])
    Object.assign(detail, res.data || {})
    loadPoints(row.id)
    loadHistory(row.id)
    loadReferralGraph(row.id)
  }).finally(() => { detailLoading.value = false })
}
function loadReferralGraph(memberId) {
  if (!memberId) { referralGraph.value = null; return }
  graphLoading.value = true
  getReferralGraph(memberId)
    .then((res) => { referralGraph.value = res.data || null })
    .catch(() => { referralGraph.value = null })
    .finally(() => { graphLoading.value = false })
}
function switchGraphFocus(memberId) {
  if (!memberId || Number(memberId) === Number(detail.id) || graphLoading.value) return
  const row = rows.value.find((it) => Number(it.id) === Number(memberId))
  openDetail(row || { id: memberId })
}
function openQuickFollow(row) {
  Object.keys(followMember).forEach((key) => delete followMember[key])
  Object.assign(followMember, row || {})
  followOpen.value = true
  if (!followForm.lookupItemId && followWays.value.length) followForm.lookupItemId = followWays.value[0].id
}
function resetFollow() {
  Object.assign(followForm, { lookupItemId: undefined, content: '', result: '', nextFollowTime: '' })
  followFormRef.value?.clearValidate()
}
function submitFollow() {
  followFormRef.value?.validate((valid) => {
    if (!valid) return
    if (!followMember.id) return proxy.$modal.msgWarning('请先选择客户')
    followSaving.value = true
    saveMemberFollow({
      memberId: Number(followMember.id),
      lookupItemId: Number(followForm.lookupItemId),
      followType: 1,
      content: followForm.content.trim(),
      result: followForm.result?.trim() || null,
      nextFollowTime: followForm.nextFollowTime || null
    }).then(() => {
      proxy.$modal.msgSuccess('客户跟进已记录')
      followOpen.value = false
      loadList()
      if (detail.id) openDetail(detail)
    }).finally(() => { followSaving.value = false })
  })
}
function loadPoints(memberId) {
  pointsLoading.value = true
  listMemberPoints({ memberId, pageNum: 1, pageSize: 8 }).then((res) => { pointRows.value = res.data?.result || [] }).finally(() => { pointsLoading.value = false })
}
function openEditor(row) {
  if (row && row.id) {
    detailLoading.value = true
    getMember(row.id).then((res) => { fillForm(res.data); editorOpen.value = true }).finally(() => { detailLoading.value = false })
  } else {
    Object.assign(form, emptyForm())
    addReferrerRow()
    editorOpen.value = true
  }
}
function fillForm(data) {
  const referrers = (data.referrers || []).map((item, index) => ({
    key: item.id || Date.now() + index,
    id: item.id,
    referrerMemberId: item.referrerMemberId,
    referrerWechatNo: item.referrerWechatNo || '',
    isPrimary: item.isPrimary === 1,
    original: true
  }))
  Object.assign(form, emptyForm(), {
    id: data.id, memberName: data.memberName || '', phone: data.phone || '', wechatNo: data.wechatNo || '', memberWechatId: data.memberWechatId || undefined, sex: data.sex ?? 0,
    solarBirthday: data.solarBirthday ? dateText(data.solarBirthday) : '',
    lunarBirthday: data.lunarBirthday || '', birthCalendar: data.birthCalendar || 1, birthYear: data.birthYear || undefined,
    occupation: data.occupation || '', hobby: data.hobby || '', wuXing: data.wuXing || '',
    recommendDirection: data.recommendDirection || '', levelId: data.levelId || undefined,
    followStatus: data.followStatus ?? 0, usualStyleItemId: data.usualStyleItemId || undefined,
    status: data.status ?? 0, remark: data.remark || '',
    referrers
  })
  if (!referrers.length && data.referrerWechatNo) {
    form.referrers.push({ key: Date.now(), referrerMemberId: data.referrerMemberId, referrerWechatNo: data.referrerWechatNo, isPrimary: true, original: true })
  }
  const primary = form.referrers.find((it) => it.isPrimary) || form.referrers[0]
  primaryReferrerKey.value = primary?.key || 0
}
function addReferrerRow() {
  const key = Date.now() + Math.floor(Math.random() * 1000)
  form.referrers.push({ key, referrerMemberId: 0, referrerWechatNo: '', isPrimary: form.referrers.length === 0, original: false })
  if (!primaryReferrerKey.value) primaryReferrerKey.value = key
}
function removeReferrerRow(index) {
  const row = form.referrers[index]
  if (!row) return
  if (row.original) {
    proxy.$modal.confirm('移出后该推荐人不再出现在当前客户的关系中，但历史销售和返现记录会保留。确定移出？')
      .then(() => { form.referrers.splice(index, 1); if (primaryReferrerKey.value === row.key) primaryReferrerKey.value = form.referrers[0]?.key || 0 })
      .catch(() => {})
    return
  }
  form.referrers.splice(index, 1)
  if (primaryReferrerKey.value === row.key) primaryReferrerKey.value = form.referrers[0]?.key || 0
}
function referrerSummary(row) {
  const list = row?.referrers || []
  if (!list.length) return '未绑定'
  return list.map((it) => (it.referrerName || it.referrerWechatNo || '未命名') + (it.isPrimary === 1 ? '（默认）' : '')).join('、')
}
function submit() {
  formRef.value?.validate((valid) => {
    if (!valid) return
    saving.value = true
    const referrers = form.referrers
      .filter((it) => String(it.referrerWechatNo || '').trim() || it.referrerMemberId > 0)
      .map((it) => ({
        id: it.id || 0,
        referrerMemberId: it.referrerMemberId || 0,
        referrerWechatNo: String(it.referrerWechatNo || '').trim() || null,
        isPrimary: it.key === primaryReferrerKey.value
      }))
    if (referrers.length && !referrers.some((it) => it.isPrimary)) referrers[0].isPrimary = true
    saveMember({
      ...form, referrers, referrersTouched: true,
      solarBirthday: form.birthCalendar === 1 ? (form.solarBirthday || null) : null,
      lunarBirthday: form.birthCalendar === 2 ? (form.lunarBirthday?.trim() || null) : null,
      birthCalendar: form.birthCalendar || 1,
      birthYear: form.birthYear || null,
      levelId: form.levelId || null,
      memberWechatId: form.memberWechatId || null,
      usualStyleItemId: form.usualStyleItemId || null
    })
      .then(() => { proxy.$modal.msgSuccess('会员档案已保存'); editorOpen.value = false; refreshAll() })
      .finally(() => { saving.value = false })
  })
}
function removeMember(row) {
  proxy.$modal.confirm('删除会员「' + row.memberName + '」？已有销售或活动记录时系统会阻止删除。').then(() => deleteMember(row.id)).then(() => {
    proxy.$modal.msgSuccess('已删除'); detailOpen.value = false; refreshAll()
  }).catch(() => {})
}
function openPoints(row) {
  Object.assign(pointsMember, row)
  Object.assign(pointsForm, { memberId: row.id, changePoints: 0, reason: '' })
  pointsOpen.value = true
}
function submitPoints() {
  if (!pointsForm.changePoints) return proxy.$modal.msgWarning('调整积分不能为 0')
  if (!pointsForm.reason.trim()) return proxy.$modal.msgWarning('请填写调整原因')
  pointsSaving.value = true
  adjustMemberPoints({ ...pointsForm }).then(() => {
    proxy.$modal.msgSuccess('积分已调整'); pointsOpen.value = false
    if (detail.id) openDetail(detail); else refreshAll()
  }).finally(() => { pointsSaving.value = false })
}
function openLevels() {
  levelsOpen.value = true
  listMemberLevels().then((res) => { levelRows.value = res.data || [] })
}
function editLevel(level) {
  Object.assign(levelForm, emptyLevel(), level ? { ...level, maxPoints: level.maxPoints ?? undefined } : {})
  levelEditOpen.value = true
}
function submitLevel() {
  levelFormRef.value?.validate((valid) => {
    if (!valid) return
    levelSaving.value = true
    saveMemberLevel({ ...levelForm, maxPoints: levelForm.maxPoints ?? null })
      .then(() => { proxy.$modal.msgSuccess('会员等级已保存'); levelEditOpen.value = false; openLevels(); loadOptions(); loadDashboard() })
      .finally(() => { levelSaving.value = false })
  })
}
function loadHistory(memberId) {
  historyLoading.value = true
  getMemberHistory(memberId)
    .then((res) => { historySales.value = res.data?.sales || [] })
    .catch(() => { historySales.value = [] })
    .finally(() => { historyLoading.value = false })
}
function openPointRule() {
  getPointRule().then((res) => {
    Object.assign(pointRule, { id: 0, amountBase: 1, pointsAwarded: 1, cashBase: 0, cashAwarded: 0, referralCashRate: 0, status: 0, remark: '' }, res.data || {})
    pointRuleOpen.value = true
  })
}
function submitPointRule() {
  if (Number(pointRule.amountBase) <= 0) return proxy.$modal.msgWarning('金额基数必须大于 0')
  if (Number(pointRule.pointsAwarded) < 0) return proxy.$modal.msgWarning('奖励积分不能为负数')
  pointRuleSaving.value = true
  savePointRule({
    id: pointRule.id || 0,
    amountBase: Number(pointRule.amountBase),
    pointsAwarded: Number(pointRule.pointsAwarded),
    cashBase: Number(pointRule.cashBase || 0),
    cashAwarded: Number(pointRule.cashAwarded || 0),
    referralCashRate: Number(pointRule.referralCashRate || 0),
    status: pointRule.status,
    remark: pointRule.remark
  }).then(() => {
    proxy.$modal.msgSuccess('积分规则已保存，新规则将在下次销售结算时生效')
    pointRuleOpen.value = false
  }).finally(() => { pointRuleSaving.value = false })
}
function clearPoints() {
  proxy.$modal.confirm('将把全库所有会员的「当前可用积分」清零。累计积分、会员等级和积分流水都会保留，不会清零。确定继续？')
    .then(() => clearAllPoints())
    .then((res) => {
      const data = res.data || {}
      proxy.$modal.msgSuccess(`已清零 ${data.memberCount || 0} 位会员，共 ${number(data.clearedPoints)} 积分，批次 ${data.batchNo || '-'}`)
      refreshAll()
      if (detail.id) loadHistory(detail.id)
    })
    .catch(() => {})
}
onMounted(refreshAll)
</script>

<style scoped lang="scss">
.jy-member { color: #3e3035; }
.member-hero { position: relative; display: flex; align-items: center; justify-content: space-between; min-height: 180px; padding: 30px 38px; overflow: hidden; background: linear-gradient(120deg, #f9edea 0%, #fdf8f4 52%, #f3dfe3 100%); border: 1px solid #f1dddd; border-radius: 20px; box-shadow: 0 12px 32px rgba(111, 66, 77, .06); }
.member-hero::before { content: ''; position: absolute; inset: 0; background-image: radial-gradient(circle at 12% 20%, rgba(255,255,255,.8) 0 1px, transparent 1px), radial-gradient(circle at 72% 80%, rgba(183,110,121,.12) 0 1px, transparent 1px); background-size: 28px 28px, 36px 36px; opacity: .65; }
.hero-copy { position: relative; z-index: 2; max-width: 640px; }.eyebrow { color: #b76e79; font-size:12px; font-weight: 700; letter-spacing: 3px; }.hero-copy h1 { margin: 10px 0 7px; color: #4a333a; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 34px; font-weight: 500; letter-spacing: 4px; }.hero-copy p { margin: 0; color: #90757d; font-size: 12px; line-height: 1.9; }.hero-actions { display: flex; gap: 10px; margin-top: 19px; }.hero-actions .el-button { margin: 0; border-radius: 8px; }
.hero-seal { position: relative; z-index: 2; display: flex; flex-direction: column; align-items: center; justify-content: center; width: 142px; height: 142px; margin-right: 28px; color: #fff; background: linear-gradient(145deg, #b76e79, #94545f); border: 5px solid rgba(255,255,255,.55); border-radius: 50%; box-shadow: 0 12px 26px rgba(144,76,89,.22); }.hero-seal span { font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2.5px; opacity: .68; }.hero-seal strong { margin: 3px 0 1px; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 35px; font-weight: 400; line-height: 1.1; }.hero-seal small { font-size:11.5px; opacity: .78; }.seal-ring { position: absolute; border: 1px solid rgba(255,255,255,.25); border-radius: 50%; }.ring-one { inset: 9px; }.ring-two { inset: 17px; border-style: dashed; }
.metric-shell { margin: -15px 18px 18px; overflow: hidden; background: #fff; border: 1px solid #f2e4e6; border-radius: 12px; box-shadow: 0 10px 26px rgba(106,68,77,.06); }
.metric-toggle { display: flex; align-items: center; justify-content: space-between; gap: 16px; width: 100%; padding: 11px 18px; color: #654c53; text-align: left; background: #fff; border: 0; cursor: pointer; }
.metric-toggle:hover { background: #fdfafb; }
.metric-toggle-main { display: flex; align-items: center; gap: 9px; min-width: 0; }
.metric-pulse { flex: none; width: 6px; height: 6px; background: #b76e79; border-radius: 50%; box-shadow: 0 0 0 4px #f9ecef; }
.metric-toggle-main b { flex: none; color: #5d434b; font-size: 12.5px; font-weight: 600; }
.metric-summary { display: grid; flex: 1; grid-template-columns: repeat(4, minmax(72px, 1fr)); gap: 16px; min-width: 0; }
.metric-summary > span { display: flex; align-items: baseline; gap: 7px; min-width: 0; }
.metric-summary small { flex: none; color: #ad959b; font-size: 11px; }
.metric-summary b { overflow: hidden; color: #654c53; font-size: 13px; font-weight: 600; font-variant-numeric: tabular-nums; text-overflow: ellipsis; white-space: nowrap; }
.metric-summary .amount b { color: #a45f6d; }
.metric-toggle-action { display: flex; flex: none; align-items: center; gap: 4px; color: #aa7b86; font-size: 11.5px; }
.metric-toggle-action em { font-style: normal; }
.metric-board { display: grid; grid-template-columns: 1.25fr 1fr 1fr 1.1fr; margin: 0; border-top: 1px solid #f5e9eb; }
.metric-board article { position: relative; min-height: 130px; padding: 20px 23px; overflow: hidden; border-left: 1px solid #f5eaec; }.metric-board article:first-child { border-left: 0; }.metric-head { display: flex; align-items: center; justify-content: space-between; color: #a98f96; font-size:12px; }.metric-head .el-icon { color: #c39a83; font-size: 17px; }.metric-board strong { display: block; margin: 11px 0 4px; color: #533c43; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 27px; font-weight: 500; }.metric-board strong em { color: #a88c93; font-family: inherit; font-size: 11px; font-style: normal; }.metric-board p { margin: 0; color: #b09ba1; font-size:11.5px; }.metric-foot { display: flex; justify-content: space-between; margin-top: 13px; color: #a98790; font-size:11.5px; }.metric-foot b { color: #b76e79; font-weight: 500; }.metric-label { color: #a98f96; font-size:12px; }.metric-board article small { display: block; margin-top: 5px; color: #b4a0a5; font-size:11.5px; }.metric-icon { position: absolute; right: 20px; bottom: 17px; display: grid; place-items: center; width: 31px; height: 31px; border-radius: 50%; }.metric-icon .el-icon { font-size: 15px; }.metric-icon.rose { color: #b76e79; background: #fbf0f1; }.metric-icon.gold { color: #b59664; background: #fbf6eb; }.metric-icon.blush { color: #ba7f82; background: #fdf1ed; }
.member-body { display: grid; grid-template-columns: 218px minmax(0, 1fr); gap: 18px; align-items: start; }
.level-rail { padding: 17px 11px 11px; background: #fff; border: 1px solid #f2e4e6; border-radius: 17px; box-shadow: 0 7px 22px rgba(106,68,77,.045); }.rail-head { display: flex; align-items: flex-end; justify-content: space-between; padding: 0 9px 12px; border-bottom: 1px solid #f8eef0; }.rail-head span { color: #c0a080; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2px; }.rail-head h2 { margin: 3px 0 0; color: #543c43; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 16px; font-weight: 500; }.rail-head > b { color: #c1a2a8; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 18px; font-weight: 400; }.level-rail > button { display: flex; align-items: center; width: 100%; padding: 11px 7px; color: #72575f; text-align: left; background: transparent; border: 0; border-radius: 9px; cursor: pointer; transition: .2s; }.level-rail > button:hover { background: #fdf6f5; }.level-rail > button.active { color: #8f5561; background: linear-gradient(90deg, #fbeef0, #fdf8f6); box-shadow: inset 2px 0 #c17e89; }.level-mark { display: grid; flex: none; place-items: center; width: 31px; height: 31px; margin-right: 9px; color: #ae858c; background: #fbf3f3; border: 1px solid #f1e1e3; border-radius: 9px; }.level-mark .el-icon { font-size: 14px; }.level-mark.all { color: #b09068; background: #fcf8f0; }.level-copy { flex: 1; min-width: 0; }.level-copy b { display: block; overflow: hidden; font-size: 11px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }.level-copy small { display: block; margin-top: 3px; overflow: hidden; color: #b29ca1; font-size:11px; text-overflow: ellipsis; white-space: nowrap; }.level-rail > button em { color: #b99da4; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 12px; font-style: normal; }.level-rail > button.active em { color: #a8616d; }.rail-note { display: flex; gap: 9px; margin: 14px 3px 2px; padding: 12px 9px; color: #a9959a; background: #fdfaf8; border: 1px dashed #efdfe1; border-radius: 9px; }.rail-note .el-icon { flex: none; margin-top: 2px; color: #c6a27c; font-size: 14px; }.rail-note p { margin: 0; font-size:11px; line-height: 1.7; }
.member-main { min-width: 0; padding: 17px; background: #fff; border: 1px solid #f2e4e6; border-radius: 17px; box-shadow: 0 7px 22px rgba(106,68,77,.045); }.toolbar { display: flex; align-items: center; justify-content: space-between; gap: 14px; padding-bottom: 15px; border-bottom: 1px solid #f7edef; }.search-box { display: flex; flex: 1; gap: 8px; min-width: 0; max-width: 820px; }.search-box .el-input { flex: 1.35; min-width: 140px; }.search-box .wechat-filter { flex: 1.15; min-width: 150px; }.search-box .el-select { flex: 1; min-width: 112px; }.search-box .el-button { flex: none; margin-left: 1px; }.toolbar-note { display: flex; align-items: center; gap: 7px; color: #aa9298; font-size:11.5px; white-space: nowrap; }.toolbar-note i { width: 5px; height: 5px; background: #c8919a; border-radius: 50%; }.toolbar-note small { color: #c0aaae; font-size:11px; }
.member-list { height: clamp(390px, calc(100vh - 520px), 680px); min-height: 390px; padding: 5px 6px 0 0; overflow-y: auto; overscroll-behavior: contain; scrollbar-gutter: stable; }.member-list.is-expanded { height: clamp(330px, calc(100vh - 660px), 560px); min-height: 330px; }.member-list::-webkit-scrollbar { width: 7px; }.member-list::-webkit-scrollbar-thumb { background: #e5d2d7; border-radius: 8px; }.member-list::-webkit-scrollbar-track { background: transparent; }.member-card { display: grid; grid-template-columns: 48px minmax(155px, 1.45fr) minmax(120px, 1fr) 112px 86px 24px; gap: 13px; align-items: center; padding: 15px 9px; border-bottom: 1px solid #fbf1f2; border-radius: 8px; cursor: pointer; transition: .2s; }.member-card:hover { background: #fdfafa; box-shadow: 0 4px 13px rgba(115,79,86,.04); transform: translateY(-1px); }.member-card.muted { opacity: .58; }.avatar { display: grid; place-items: center; width: 43px; height: 43px; color: #9f626d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; background: linear-gradient(140deg, #fdf0ef, #f5dde1); border: 1px solid #f0d8dd; border-radius: 50%; }.avatar-2 { color: #9c815a; background: linear-gradient(140deg, #fcf6e9, #f1e5cb); }.avatar-3 { color: #92776d; background: linear-gradient(140deg, #f7ebe6, #e9d9d0); }.name-line { display: flex; align-items: center; gap: 7px; min-width: 0; }.name-line h3 { margin: 0; overflow: hidden; color: #543a42; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 15px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }.level-tag, .off-tag { flex: none; padding: 2px 6px; color: #ac737d; font-size:11px; background: #fbf0f1; border-radius: 3px; }.off-tag { color: #a7989c; background: #f5f2f2; }.identity > p .wx-line { margin-left: 8px; color: #a98a92; }.identity > p { margin: 4px 0 6px; color: #b29ca1; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; }.mini-tags { display: flex; gap: 5px; }.mini-tags span { padding: 2px 5px; color: #ad9298; font-size:11px; background: #fcf8f7; border: 1px solid #f6ebec; border-radius: 3px; }.member-value span, .member-points span, .member-follow > span { display: block; color: #b39da2; font-size:11px; }.member-value b, .member-points b { display: block; margin: 3px 0 2px; color: #624850; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 13px; font-weight: 500; }.member-value small, .member-points small { color: #bba8ac; font-size:11px; }.member-points b { color: #9b7a58; }.member-points b em { color: #9b7a58; font-family: inherit; font-size:11px; font-style: normal; font-weight: 400; }.member-points small { white-space: nowrap; }.member-follow { padding-left: 10px; border-left: 1px solid #f5eaec; }.member-follow i { display: inline-block; width: 5px; height: 5px; margin-right: 5px; background: #c897a0; border-radius: 50%; }.member-follow > span { display: inline; font-size:11.5px; }.member-follow small { display: block; margin-top: 5px; color: #bba8ac; font-size:11px; }.member-follow.follow-2 i { background: #b8a891; }.member-follow.follow-3 i { background: #8eae99; }.card-arrow { display: grid; place-items: center; width: 24px; height: 24px; color: #c9b5b9; background: transparent; border: 0; border-radius: 6px; }.card-arrow:hover { color: #a5636e; background: #fbf0f1; }.empty-member { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 370px; }.empty-icon { display: grid; place-items: center; width: 57px; height: 57px; color: #b88977; background: #fbf1ee; border-radius: 50%; }.empty-icon .el-icon { font-size: 24px; }.empty-member h3 { margin: 14px 0 5px; color: #574047; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 18px; font-weight: 500; }.empty-member p { margin: 0 0 16px; color: #a58e94; font-size:12px; }.list-foot { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 11px 0 0; border-top: 1px solid #f8eef0; }.list-foot > span { color: #b8a4a8; font-size:11px; }
.drawer-heading { display: flex; align-items: center; gap: 7px; padding: 2px 2px 18px; }.drawer-heading span { color: #c0a080; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2px; }.drawer-heading b { color: #4b363d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; }.drawer-heading button { display: grid; place-items: center; width: 28px; height: 28px; margin-left: auto; color: #bba6aa; background: transparent; border: 0; border-radius: 7px; }.drawer-heading button:hover { color: #9f606b; background: #fbf0f1; }
.detail-wrap, .editor-wrap { padding: 2px 4px 15px; }.portrait { display: flex; align-items: center; gap: 14px; padding: 17px; background: linear-gradient(135deg, #fdf5f3, #faf0f2); border: 1px solid #f1dfe2; border-radius: 15px; }.portrait-avatar { display: grid; flex: none; place-items: center; width: 57px; height: 57px; color: #9f626d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 25px; background: #fff; border: 1px solid #ecd6d9; border-radius: 50%; box-shadow: 0 5px 13px rgba(127,74,85,.08); }.portrait-main { flex: 1; min-width: 0; }.portrait-title { display: flex; align-items: center; gap: 8px; }.portrait-title h2 { margin: 0; color: #4b353c; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 22px; font-weight: 500; }.portrait-title span { padding: 3px 7px; color: #a96671; font-size:11.5px; background: #fff; border-radius: 3px; }.portrait-main p { margin: 5px 0 7px; color: #ad969c; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; }.portrait-tags { display: flex; gap: 5px; }.portrait-tags span { padding: 2px 6px; color: #a98d93; font-size:11px; background: rgba(255,255,255,.68); border-radius: 3px; }.more-btn { display: grid; place-items: center; width: 27px; height: 27px; color: #b9a3a8; background: transparent; border: 0; border-radius: 7px; }.more-btn:hover { background: #fff; }.portrait-stats { display: grid; grid-template-columns: repeat(5, 1fr); margin: 14px 0 22px; background: #fff; border: 1px solid #f2e5e7; border-radius: 12px; }.portrait-stats div { padding: 12px 10px; border-left: 1px solid #f7edef; }.portrait-stats div:first-child { border-left: 0; }.portrait-stats span { display: block; color: #b29ba1; font-size:11px; }.portrait-stats strong { display: block; margin-top: 5px; color: #61484f; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 15px; font-weight: 500; }.portrait-stats em { color: #a98f95; font-size:11px; font-style: normal; }.portrait-stats div:nth-child(3) strong, .portrait-stats div:nth-child(4) strong { color: #9b7a58; }.detail-section { margin-bottom: 22px; }.section-caption { display: flex; align-items: flex-end; gap: 7px; margin-bottom: 10px; padding-bottom: 9px; border-bottom: 1px solid #f2e4e6; }.section-caption span { color: #c0a080; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2px; }.section-caption h3 { margin: 0; color: #513a41; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 16px; font-weight: 500; }.section-caption.with-action { justify-content: space-between; }.section-caption.with-action > div { display: flex; align-items: flex-end; gap: 7px; }.info-grid { display: grid; grid-template-columns: 1fr 1fr; background: #fff; border: 1px solid #f2e5e7; border-radius: 12px; overflow: hidden; }.info-grid div { padding: 11px 13px; border-bottom: 1px solid #f8eff0; }.info-grid div:nth-child(odd) { border-right: 1px solid #f8eff0; }.info-grid div:nth-last-child(-n+2) { border-bottom: 0; }.info-grid span { display: block; color: #b39ca2; font-size:11px; }.info-grid b { display: block; margin-top: 4px; color: #654d54; font-size:12px; font-weight: 500; }.soft-note { display: flex; gap: 10px; margin-top: 9px; padding: 10px 12px; background: #fdfaf9; border: 1px solid #f7eded; border-radius: 8px; }.soft-note > span { flex: none; color: #b59a9f; font-size:11px; }.soft-note p { margin: 0; color: #846e74; font-size:11.5px; line-height: 1.7; }
.point-list { min-height: 75px; }.point-row { display: flex; align-items: center; gap: 10px; padding: 10px 2px; border-bottom: 1px solid #faf2f3; }.point-row > i { width: 7px; height: 7px; border-radius: 50%; }.point-row > i.up { background: #7fa78d; box-shadow: 0 0 0 4px #f0f8f3; }.point-row > i.down { background: #b77b85; box-shadow: 0 0 0 4px #fbf1f2; }.point-row > div { flex: 1; }.point-row b { display: block; color: #5c444b; font-size:12px; font-weight: 500; }.point-row small { display: block; margin-top: 3px; color: #b39da2; font-size:11px; }.point-row strong { font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 13px; font-weight: 500; }.point-row strong.up { color: #628e75; }.point-row strong.down { color: #a9636e; }.points-empty { display: flex; flex-direction: column; align-items: center; padding: 25px 0; color: #b7a2a7; }.points-empty .el-icon { color: #cbb49a; font-size: 21px; }.points-empty p { margin: 5px 0 0; font-size:11.5px; }
.form-section { margin-bottom: 21px; }.form-caption { display: flex; align-items: center; gap: 8px; margin-bottom: 13px; padding-bottom: 9px; border-bottom: 1px solid #f2e4e6; }.form-caption span { display: grid; place-items: center; width: 20px; height: 20px; color: #b76e79; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; background: #fbf0f1; border-radius: 50%; }.form-caption b { color: #5a4048; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 500; }.form-grid { display: grid; gap: 0 14px; }.form-grid.two { grid-template-columns: 1fr 1fr; }.member-form :deep(.el-form-item) { margin-bottom: 14px; }.member-form :deep(.el-form-item__label) { height: auto; padding-bottom: 4px; color: #967d84; font-size:11.5px; }.member-form :deep(.el-input__wrapper), .member-form :deep(.el-textarea__inner), .member-form :deep(.el-select__wrapper) { border-radius: 7px; box-shadow: 0 0 0 1px #efdfe2 inset; }.member-form :deep(.el-input__wrapper.is-focus), .member-form :deep(.el-select__wrapper.is-focused) { box-shadow: 0 0 0 1px #c17a86 inset; }.member-form :deep(.el-radio-group) { gap: 12px; }.field-hint { margin: 3px 0 0; color: #b29ca1; font-size:11px; line-height: 1.5; }.editor-actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 14px; border-top: 1px solid #f2e4e6; }.editor-actions .el-button { margin-left: 0; }
.dialog-intro { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; padding: 12px 14px; background: #fdf7f5; border: 1px solid #f3e5e4; border-radius: 8px; }.dialog-intro span { color: #674d54; font-size: 12px; font-weight: 500; }.dialog-intro small { color: #a98f95; font-size:11.5px; }.dialog-lead { margin: -5px 0 14px; color: #9e858c; font-size:11.5px; line-height: 1.7; }.level-table { border: 1px solid #f2e4e6; border-radius: 10px; overflow: hidden; }.level-table-head, .level-table-row { display: grid; grid-template-columns: 1.55fr 1.35fr .8fr .9fr .7fr 55px; gap: 8px; align-items: center; padding: 10px 12px; }.level-table-head { color: #ad9298; font-size:11.5px; background: #fdf8f7; border-bottom: 1px solid #f2e4e6; }.level-table-row { color: #765d64; font-size:11.5px; border-bottom: 1px solid #f8eef0; }.level-table-row:last-child { border-bottom: 0; }.level-name { display: flex; align-items: center; gap: 5px; color: #624850; font-size:12px; }.level-name .el-icon { color: #b58d7d; }.level-name small { margin-left: 2px; color: #b8a4a8; font-size:11px; }.level-table-row .on { color: #689078; }.level-table-row .off { color: #b59da2; }.level-table-row .el-button { padding: 0; font-size:11.5px; }
@media (max-width: 1280px) { .metric-board { grid-template-columns: 1.2fr 1fr 1fr; }.metric-board article:nth-child(4) { border-top: 1px solid #f5e9ea; border-left: 0; }.member-card { grid-template-columns: 48px minmax(150px, 1.5fr) minmax(110px, 1fr) 108px 24px; }.member-follow { display: none; }.toolbar { align-items: flex-start; flex-direction: column; }.search-box { width: 100%; max-width: none; }.toolbar-note { padding-left: 2px; } }
@media (max-width: 900px) { .member-hero { align-items: flex-start; flex-direction: column; padding: 24px; }.hero-seal { display: none; }.metric-shell { margin: -12px 8px 14px; }.metric-toggle { flex-wrap: wrap; gap: 9px; padding: 10px 12px; }.metric-summary { order: 3; flex-basis: 100%; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 7px 14px; padding-top: 9px; border-top: 1px solid #f7edef; }.metric-summary > span { justify-content: space-between; }.metric-summary small { font-size: 10.5px; }.metric-summary b { font-size: 12.5px; }.metric-board { grid-template-columns: 1fr 1fr; }.metric-board article:nth-child(3) { border-left: 0; border-top: 1px solid #f5e9ea; }.metric-board article:nth-child(4) { border-top: 1px solid #f5e9ea; }.member-body { grid-template-columns: 1fr; }.level-rail { display: flex; flex-wrap: wrap; gap: 3px; padding: 12px; }.rail-head { width: 100%; }.level-rail > button { width: auto; min-width: 140px; flex: 1; }.rail-note { width: 100%; margin-top: 6px; }.member-main { padding: 13px; }.search-box { flex-wrap: wrap; }.search-box .el-input { flex: 1 1 100%; }.search-box .el-select { flex: 1; }.member-list, .member-list.is-expanded { height: 58vh; min-height: 360px; }.member-card { grid-template-columns: 42px 1fr 24px; gap: 9px; padding: 12px 4px; }.avatar { width: 38px; height: 38px; }.member-value, .member-points { display: none; }.list-foot { align-items: flex-start; flex-direction: column; }.form-grid.two { grid-template-columns: 1fr; }.portrait-stats { grid-template-columns: 1fr 1fr; }.portrait-stats div:nth-child(n+3) { border-top: 1px solid #f7edef; }.portrait-stats div:nth-child(odd) { border-left: 0; }.portrait-stats div:nth-child(5) { grid-column: 1 / -1; }.level-table-head, .level-table-row { grid-template-columns: 1.3fr 1.2fr .7fr 45px; }.level-table-head span:nth-child(4), .level-table-row span:nth-child(4), .level-table-head span:nth-child(5), .level-table-row span:nth-child(5) { display: none; } }
.cash-rule-block { margin: 4px 0 14px; padding: 12px 14px; background: #fdf7f8; border: 1px solid #f3e2e6; border-radius: 8px; }
.cash-rule-head { display: flex; align-items: baseline; gap: 8px; margin-bottom: 8px; }
.cash-rule-head span { color: #9d5966; font-size: 13px; font-weight: 600; }
.cash-rule-head small { color: #b09aa0; font-size: 11.5px; }
.cash-rule-block .el-form-item { margin-bottom: 6px; }
.referrer-editor { display: flex; flex-direction: column; gap: 6px; width: 100%; }
.referrer-row { display: grid; grid-template-columns: minmax(0, 1fr) auto 28px; gap: 8px; align-items: center; }
.referrer-row .primary-radio { margin-right: 0; color: #8e6c74; }
.referrer-row .primary-radio .el-radio__label { padding-left: 4px; font-size: 12px; }
.referrer-editor > .el-button { align-self: flex-start; padding-left: 0; }
</style>


<style scoped>
.history-count { color: #b28f97; font-size:12px; }
.history-list { display: flex; flex-direction: column; gap: 7px; }
.lineage-panel { padding: 13px; background: linear-gradient(145deg, #fffafb, #fdf6f7); border: 1px solid #f1e1e4; border-radius: 13px; }
.mindmap-toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; padding: 2px 2px 10px; color: #9d7e86; font-size: 10.5px; }
.mindmap-toolbar span { display: inline-flex; align-items: center; gap: 4px; }
.mindmap-toolbar small { margin-left: auto; color: #b39da3; }
.mindmap-toolbar .dot { display: inline-block; width: 7px; height: 7px; border-radius: 50%; }
.mindmap-toolbar .dot.up { background: #c5a06e; }
.mindmap-toolbar .dot.self { background: #b76e79; }
.mindmap-toolbar .dot.down { background: #8a9d86; }
.mindmap-scroll { overflow-x: auto; overflow-y: hidden; padding: 2px 1px 6px; }
.mindmap { display: grid; grid-template-columns: minmax(120px, 1fr) 132px minmax(120px, 1fr); align-items: center; min-width: 500px; padding: 4px 0; }
.up-side::after, .down-side::before { content: ''; position: absolute; top: 50%; width: 1px; height: calc(100% - 34px); min-height: 20px; background: #e3d0d5; transform: translateY(-50%); }
.up-side::after { right: 0; }
.down-side::before { left: 0; }
.mindmap-side { position: relative; display: flex; flex-direction: column; justify-content: center; gap: 8px; min-width: 0; }
.up-side { align-items: flex-end; padding-right: 14px; }
.down-side { align-items: flex-start; padding-left: 14px; }
.focus-side { position: relative; z-index: 1; align-items: center; }
.mindmap-level { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.up-level { align-items: flex-end; }
.down-level { align-items: flex-start; }
.mindmap-level-label { color: #b19aa0; font-size: 9.5px; letter-spacing: .5px; }
.focus-label { padding-bottom: 4px; color: #a45f6e; font-size: 10px; font-weight: 700; }
.mindmap-nodes { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.mind-node { position: relative; display: flex; flex-direction: column; gap: 2px; width: 100%; min-width: 0; padding: 7px 9px; color: #624951; text-align: left; background: #fff; border: 1px solid #f0e2e5; border-radius: 7px; cursor: pointer; transition: border-color .16s, box-shadow .16s, transform .16s; }
.mind-node::after { content: ''; position: absolute; top: 50%; width: 14px; height: 1px; background: #dcc5ca; }
.mind-node.up::after { right: -14px; }
.mind-node.down::after { left: -14px; }
.mind-node.up::before, .mind-node.down::before { content: ''; position: absolute; top: 50%; width: 5px; height: 5px; margin-top: -2px; background: #fff; border: 1px solid #d4b7be; border-radius: 50%; }
.mind-node.up::before { right: -16px; }
.mind-node.down::before { left: -16px; }
.mind-node:hover { border-color: #d9b4bd; box-shadow: 0 4px 12px rgba(167,94,111,.1); transform: translateY(-1px); }
.mind-node.up { border-left: 3px solid #c5a06e; }
.mind-node.down { border-left: 3px solid #8a9d86; }
.mind-node.focus { width: 100%; min-height: 84px; padding: 11px; border: 1px solid #dbb9c1; background: linear-gradient(145deg, #fff, #fff4f6); box-shadow: 0 6px 16px rgba(167,94,111,.11); }
.mind-node.focus::before, .mind-node.focus::after { display: none; }
.mind-node-top { display: flex; align-items: center; gap: 5px; min-width: 0; }
.mind-node-top em { flex: none; padding: 1px 4px; color: #aa7883; background: #fbf0f2; border-radius: 3px; font-size: 8.5px; font-style: normal; }
.mind-node-top i { overflow: hidden; color: #624951; font-size: 11.5px; font-style: normal; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.mind-node small { overflow: hidden; color: #ad969c; font-size: 9.5px; text-overflow: ellipsis; white-space: nowrap; }
.mind-node-foot { overflow: hidden; color: #bfa9ae; font-size: 9px; text-overflow: ellipsis; white-space: nowrap; }
.mind-focus-stats { padding-top: 4px; color: #a45f6e; border-top: 1px dashed #efdbe0; font-size: 9.5px; }
.mindmap-empty { padding: 12px 8px; color: #b6a0a5; background: rgba(255,255,255,.5); border: 1px dashed #ecdde0; border-radius: 7px; font-size: 10px; text-align: center; }
.lineage-empty { padding: 13px 7px; color: #b29ca1; font-size: 10.5px; text-align: center; background: rgba(255,255,255,.55); border: 1px dashed #ecdde0; border-radius: 7px; }
.lineage-empty.large { padding: 22px 10px; }
@media (max-width: 640px) {
  .mindmap-toolbar small { width: 100%; margin-left: 0; }
  .mindmap { grid-template-columns: minmax(100px, 1fr) 118px minmax(100px, 1fr); min-width: 420px; }
  .up-side { padding-right: 9px; }
  .down-side { padding-left: 9px; }
  .mind-node { padding: 6px 7px; }
  .mind-node.focus { padding: 9px; }
}
.history-row { display: flex; align-items: center; gap: 10px; padding: 10px 12px; background: #fdf8f8; border: 1px solid #f4e6e8; border-radius: 9px; }
.history-dot { width: 8px; height: 8px; border-radius: 50%; flex: 0 0 8px; background: #cfb2b8; }
.history-dot.done { background: #c4788a; box-shadow: 0 0 0 3px rgba(196, 120, 138, 0.14); }
.history-dot.draft { background: #e0b978; box-shadow: 0 0 0 3px rgba(224, 185, 120, 0.16); }
.history-copy { flex: 1 1 auto; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.history-copy b { color: #5a4048; font-size: 11px; font-weight: 500; }
.history-copy small { color: #ad979c; font-size:11.5px; }
.history-row strong { color: #a9636e; font-family: "Microsoft YaHei", "微软雅黑", sans-serif; font-size: 13px; font-weight: 500; }
.cash-rule-block { margin: 4px 0 14px; padding: 12px 14px; background: #fdf7f8; border: 1px solid #f3e2e6; border-radius: 8px; }
.cash-rule-head { display: flex; align-items: baseline; gap: 8px; margin-bottom: 8px; }
.cash-rule-head span { color: #9d5966; font-size: 13px; font-weight: 600; }
.cash-rule-head small { color: #b09aa0; font-size: 11.5px; }
.cash-rule-block .el-form-item { margin-bottom: 6px; }
.member-wechat-field { grid-column: 1 / -1; }
.member-wechat-field :deep(.el-select) { width: 100%; }
</style>

<style scoped>
/* 客户小档案：卡片常显偏好与最近跟进，信息在不展开时也可读。 */
.member-card { grid-template-rows: auto auto; align-items: start; }
.member-card > .avatar, .member-card > .identity, .member-card > .member-value, .member-card > .member-points, .member-card > .member-follow, .member-card > .card-arrow { grid-row: 1; }
.preference-strip { grid-column: 2 / -1; display: flex; flex-wrap: wrap; align-items: center; gap: 6px; min-width: 0; padding-top: 10px; margin-top: -2px; border-top: 1px dashed #f4e6e9; }
.preference-label { flex: none; color: #a88f95; font-size: 11px; }
.preference-chip { display: inline-flex; align-items: center; gap: 4px; padding: 3px 8px; color: #8d4f5d; background: #fdf1f3; border: 1px solid #f4dfe3; border-radius: 999px; font-size: 11px; white-space: nowrap; }
.preference-chip em { color: #b17f88; font-style: normal; }
.preference-empty { color: #b39da2; font-size: 11px; }
.card-quick-actions { display: flex; gap: 5px; margin-left: auto; }
.card-quick-actions button { display: inline-flex; align-items: center; gap: 4px; padding: 4px 8px; color: #9c6672; background: #fff; border: 1px solid #f0dfe3; border-radius: 6px; font-size: 11px; cursor: pointer; }
.card-quick-actions button:hover { color: #984f5f; background: #fff7f8; border-color: #e6bfc7; }
.follow-head { display: flex; align-items: center; white-space: nowrap; }
.follow-snippet { display: -webkit-box; overflow: hidden; margin: 4px 0 0; color: #9e868d; font-size: 11px; line-height: 1.55; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.preference-panel { display: flex; flex-direction: column; gap: 9px; }
.preference-panel article { display: grid; grid-template-columns: 130px minmax(80px, 1fr) 86px; gap: 12px; align-items: center; padding: 10px 12px; background: #fff; border: 1px solid #f2e5e7; border-radius: 9px; }
.preference-name { display: flex; flex-direction: column; gap: 3px; }
.preference-name b { color: #61474f; font-size: 13px; font-weight: 600; }
.preference-name small { color: #a88e94; font-size: 11px; }
.preference-track { height: 7px; overflow: hidden; background: #f8ebee; border-radius: 99px; }
.preference-track i { display: block; height: 100%; background: linear-gradient(90deg, #e3b7c0, #b96274); border-radius: inherit; }
.preference-panel strong { color: #9d5261; font-size: 13px; font-weight: 600; text-align: right; }
.follow-summary { display: flex; gap: 10px; padding: 12px; background: #fff; border: 1px solid #f2e5e7; border-radius: 10px; }
.follow-badge { align-self: flex-start; flex: none; padding: 4px 8px; color: #a25868; background: #fdf0f3; border-radius: 5px; font-size: 11px; }
.follow-content { min-width: 0; }
.follow-content b { color: #a1848b; font-size: 11px; font-weight: 400; }
.follow-content p { margin: 5px 0; color: #60474e; font-size: 13px; line-height: 1.7; white-space: pre-wrap; }
.follow-content small { color: #b19ba0; font-size: 11px; }
.follow-target { display: flex; align-items: center; gap: 10px; margin-bottom: 18px; padding: 13px 14px; background: #fdf6f7; border: 1px solid #f2e1e5; border-radius: 9px; }
.follow-target-avatar { display: grid; flex: none; place-items: center; width: 38px; height: 38px; color: #a4606d; background: #fff; border-radius: 50%; font-size: 16px; }
.follow-target > div:nth-child(2) { flex: 1; min-width: 0; }
.follow-target b { color: #5d434b; font-size: 14px; font-weight: 600; }
.follow-target p { margin: 3px 0 0; color: #a88f95; font-size: 11px; }
.follow-target > span { color: #a46b55; font-size: 12px; }
.follow-form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.follow-tip { display: flex; align-items: center; gap: 6px; padding: 8px 10px; color: #9d858b; background: #fffaf8; border-radius: 6px; font-size: 11px; }
@media (max-width: 1280px) {
  .member-card { grid-template-columns: 48px minmax(150px, 1.5fr) minmax(110px, 1fr) 108px 24px; }
  .member-card > .member-follow { display: none; }
  .preference-strip { grid-column: 2 / -1; }
}
@media (max-width: 900px) {
  .member-card { grid-template-columns: 42px 1fr 24px; }
  .member-card > .member-value, .member-card > .member-points { display: none; }
  .preference-strip { grid-column: 1 / -1; }
  .card-quick-actions { width: 100%; margin-left: 0; }
  .follow-form-grid { grid-template-columns: 1fr; gap: 0; }
}
.search-box .sort-filter, .search-box .direction-filter { flex: 0 0 auto; }
@media (max-width: 900px) {
  .search-box .sort-filter, .search-box .direction-filter { flex: 1 1 110px; }
}
.cash-rule-block { margin: 4px 0 14px; padding: 12px 14px; background: #fdf7f8; border: 1px solid #f3e2e6; border-radius: 8px; }
.cash-rule-head { display: flex; align-items: baseline; gap: 8px; margin-bottom: 8px; }
.cash-rule-head span { color: #9d5966; font-size: 13px; font-weight: 600; }
.cash-rule-head small { color: #b09aa0; font-size: 11.5px; }
.cash-rule-block .el-form-item { margin-bottom: 6px; }
.search-box .sort-filter { flex: 0 0 118px; min-width: 118px; }
.search-box .direction-filter { flex: 0 0 82px; min-width: 82px; }
.mini-tags .create-time { color: #a8978b; background: #fbf8f2; border-color: #f0e8da; }
.identity-copy { display: flex; flex-wrap: wrap; align-items: center; gap: 5px; }
.identity-copy .copy-field { display: inline-flex; align-items: center; gap: 1px; }
.copy-code { display: inline-grid; flex: none; place-items: center; width: 21px; height: 21px; padding: 0; color: #b89fa4; background: transparent; border: 0; border-radius: 4px; cursor: pointer; }
.copy-code:hover { color: #9b5364; background: #fff; }
</style>




