import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  CloseCircleOutlined,
  CloudSyncOutlined,
  DatabaseOutlined,
  FileMarkdownOutlined,
  PlayCircleOutlined,
  PlusOutlined,
  ReloadOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Descriptions,
  Drawer,
  Empty,
  Form,
  Input,
  Layout,
  List,
  Modal,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Timeline,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'
import type { ReactNode } from 'react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import './App.css'
import {
  approveReview,
  createTask,
  getInput,
  getTask,
  getTrace,
  listPendingReviews,
  listTasks,
  modifyReview,
  rejectReview,
  startTask,
  verifyEvidence,
} from './api'
import type {
  AiTask,
  Artifact,
  EvidenceItem,
  ReviewItem,
  TaskDetail,
  TraceEvent,
} from './types'

const { Header, Sider, Content } = Layout

const sampleMaterials =
  '测试科技股份有限公司主营企业软件。2024年营业收入 12 亿元，净利润 1.2 亿元，总资产 30 亿元，总负债 16 亿元，流动资产 10 亿元，流动负债 8 亿元。'

function App() {
  const [tasks, setTasks] = useState<AiTask[]>([])
  const [selectedTaskId, setSelectedTaskId] = useState<string>()
  const [detail, setDetail] = useState<TaskDetail>()
  const [pendingReviews, setPendingReviews] = useState<ReviewItem[]>([])
  const [trace, setTrace] = useState<TraceEvent[]>([])
  const [createOpen, setCreateOpen] = useState(false)
  const [reportOpen, setReportOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const selectedTaskIdRef = useRef<string | undefined>(undefined)
  const [form] = Form.useForm()

  const selectedInput = useMemo(() => getInput(detail?.task), [detail?.task])
  const markdownArtifact = detail?.artifacts.find((artifact) => artifact.artifactType === 'Markdown')
  const markdown = getMarkdown(markdownArtifact)

  const refresh = useCallback(async (nextTaskId?: string) => {
    setBusy(true)
    try {
      const [taskData, reviewData] = await Promise.all([listTasks(), listPendingReviews()])
      setTasks(taskData.items)
      setPendingReviews(reviewData.items)

      const taskId = nextTaskId ?? selectedTaskIdRef.current ?? taskData.items[0]?.id
      selectedTaskIdRef.current = taskId
      setSelectedTaskId(taskId)
      if (taskId) {
        const [taskDetail, traceData] = await Promise.all([getTask(taskId), getTrace(taskId)])
        setDetail(taskDetail)
        setTrace(traceData.items)
      } else {
        setDetail(undefined)
        setTrace([])
      }
    } catch (error) {
      showError(error)
    } finally {
      setBusy(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void refresh()
    }, 0)
    return () => window.clearTimeout(timer)
  }, [refresh])

  async function handleCreate(values: {
    companyName: string
    title: string
    period: string
    reportType: string
    materialsText: string
  }) {
    setBusy(true)
    try {
      const created = await createTask({
        scenarioCode: 'credit_rating',
        title: values.title,
        input: {
          company_name: values.companyName,
          period: values.period,
          report_type: values.reportType,
          materials_text: values.materialsText,
        },
      })
      setCreateOpen(false)
      form.resetFields()
      await refresh(created.id)
      message.success('任务已创建')
    } catch (error) {
      showError(error)
    } finally {
      setBusy(false)
    }
  }

  async function handleStart(taskId: string) {
    setBusy(true)
    try {
      await startTask(taskId)
      await refresh(taskId)
      message.success('Workflow 已启动')
    } catch (error) {
      showError(error)
    } finally {
      setBusy(false)
    }
  }

  async function handleReview(action: 'approve' | 'reject' | 'modify', review: ReviewItem) {
    const comment = action === 'reject' ? '驳回，需补充证据' : '同意进入下一步'
    setBusy(true)
    try {
      if (action === 'approve') {
        await approveReview(review.id, comment)
      } else if (action === 'reject') {
        await rejectReview(review.id, comment)
      } else {
        await modifyReview(review.id, '修改后同意', safeParse(review.contentJson))
      }
      await refresh(review.taskId)
      message.success('审核已处理')
    } catch (error) {
      showError(error)
    } finally {
      setBusy(false)
    }
  }

  async function handleVerifyEvidence(evidenceId: string) {
    setBusy(true)
    try {
      await verifyEvidence(evidenceId)
      await refresh(selectedTaskId)
      message.success('证据已标记')
    } catch (error) {
      showError(error)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Layout className="shell">
      <Header className="topbar">
        <div className="brand">
          <SafetyCertificateOutlined />
          <span>AI Business Orchestrator</span>
        </div>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={() => void refresh()} loading={busy}>
            刷新
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
            新建信评任务
          </Button>
        </Space>
      </Header>

      <Layout className="workspace">
        <Sider width={330} className="task-rail">
          <div className="rail-head">
            <Typography.Title level={4}>任务队列</Typography.Title>
            <Tag color="cyan">{tasks.length}</Tag>
          </div>
          <List
            loading={busy && tasks.length === 0}
            dataSource={tasks}
            locale={{ emptyText: '暂无任务' }}
            renderItem={(task) => (
              <List.Item
                className={task.id === selectedTaskId ? 'task-row active' : 'task-row'}
                onClick={() => void refresh(task.id)}
              >
                <div className="task-row-title">{task.title}</div>
                <div className="task-row-meta">
                  <StatusTag status={task.status} />
                  <span>{formatDate(task.createdAt)}</span>
                </div>
              </List.Item>
            )}
          />
        </Sider>

        <Content className="content">
          {detail ? (
            <>
              <section className="summary-band">
                <div>
                  <Typography.Title level={2}>{detail.task.title}</Typography.Title>
                  <Space wrap>
                    <StatusTag status={detail.task.status} />
                    <Tag>{detail.task.scenarioCode}</Tag>
                    {detail.workflowRun ? <Tag>{detail.workflowRun.workflowCode}</Tag> : null}
                  </Space>
                </div>
                <Space wrap>
                  {detail.task.status === 'Created' ? (
                    <Button
                      type="primary"
                      icon={<PlayCircleOutlined />}
                      loading={busy}
                      onClick={() => void handleStart(detail.task.id)}
                    >
                      启动
                    </Button>
                  ) : null}
                  {markdownArtifact ? (
                    <Button icon={<FileMarkdownOutlined />} onClick={() => setReportOpen(true)}>
                      报告
                    </Button>
                  ) : null}
                </Space>
              </section>

              <section className="metric-grid">
                <Statistic title="步骤" value={detail.steps.length} prefix={<CloudSyncOutlined />} />
                <Statistic title="待审" value={pendingReviews.length} prefix={<ClockCircleOutlined />} />
                <Statistic title="证据" value={detail.evidence.length} prefix={<DatabaseOutlined />} />
                <Statistic title="产物" value={detail.artifacts.length} prefix={<FileMarkdownOutlined />} />
              </section>

              <Tabs
                className="work-tabs"
                items={[
                  {
                    key: 'overview',
                    label: '流程',
                    children: (
                      <div className="two-column">
                        <section className="surface">
                          <Typography.Title level={4}>步骤执行</Typography.Title>
                          <Timeline
                            items={detail.steps.map((step) => ({
                              color: timelineColor(step.status),
                              dot: step.status === 'Failed' ? <CloseCircleOutlined /> : undefined,
                              children: (
                                <div className="timeline-item">
                                  <Space wrap>
                                    <strong>{step.stepName ?? step.stepId}</strong>
                                    <Tag>{step.stepType}</Tag>
                                    <StatusTag status={step.status} />
                                  </Space>
                                  {step.errorMessage ? <Alert type="error" message={step.errorMessage} /> : null}
                                  {step.outputJson ? (
                                    <StepBusinessOutput stepId={step.stepId} output={step.outputJson} evidence={detail.evidence} />
                                  ) : null}
                                </div>
                              ),
                            }))}
                          />
                        </section>
                        <section className="surface">
                          <Typography.Title level={4}>任务输入</Typography.Title>
                          <TaskInputSummary input={selectedInput} />
                        </section>
                      </div>
                    ),
                  },
                  {
                    key: 'review',
                    label: '审核',
                    children: (
                      <ReviewTable
                        reviews={detail.reviews}
                        evidence={detail.evidence}
                        busy={busy}
                        onAction={handleReview}
                      />
                    ),
                  },
                  {
                    key: 'evidence',
                    label: '证据',
                    children: (
                      <EvidenceTable evidence={detail.evidence} busy={busy} onVerify={handleVerifyEvidence} />
                    ),
                  },
                  {
                    key: 'artifact',
                    label: '产物',
                    children: <ArtifactTable artifacts={detail.artifacts} onOpenReport={() => setReportOpen(true)} />,
                  },
                  {
                    key: 'trace',
                    label: 'Trace',
                    children: <TraceTable trace={trace} />,
                  },
                ]}
              />
            </>
          ) : (
            <section className="empty-state">
              <Typography.Title level={3}>暂无任务</Typography.Title>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
                新建信评任务
              </Button>
            </section>
          )}
        </Content>
      </Layout>

      <Drawer
        title="新建信评任务"
        width={560}
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            companyName: '测试科技股份有限公司',
            title: '测试科技信评报告',
            period: '2024',
            reportType: '主体信用分析',
            materialsText: sampleMaterials,
          }}
          onFinish={handleCreate}
        >
          <Form.Item name="companyName" label="公司名称" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="title" label="任务标题" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Space className="form-row">
            <Form.Item name="period" label="期间" rules={[{ required: true }]}>
              <Input />
            </Form.Item>
            <Form.Item name="reportType" label="报告类型" rules={[{ required: true }]}>
              <Input />
            </Form.Item>
          </Space>
          <Form.Item name="materialsText" label="资料文本" rules={[{ required: true }]}>
            <Input.TextArea rows={10} />
          </Form.Item>
          <Button type="primary" htmlType="submit" block loading={busy}>
            创建
          </Button>
        </Form>
      </Drawer>

      <Modal
        title={markdownArtifact?.name ?? '报告草稿'}
        open={reportOpen}
        onCancel={() => setReportOpen(false)}
        footer={null}
        width={920}
      >
        <div className="markdown-preview">
          <ReactMarkdown>{markdown}</ReactMarkdown>
        </div>
      </Modal>
    </Layout>
  )
}

function TaskInputSummary({ input }: { input: Record<string, unknown> }) {
  return (
    <Descriptions bordered size="small" column={1} className="business-descriptions">
      <Descriptions.Item label="公司名称">{String(input.company_name ?? '-')}</Descriptions.Item>
      <Descriptions.Item label="报告期间">{String(input.period ?? '-')}</Descriptions.Item>
      <Descriptions.Item label="报告类型">{String(input.report_type ?? '-')}</Descriptions.Item>
      <Descriptions.Item label="资料摘要">
        <span className="long-text">{String(input.materials_text ?? '-')}</span>
      </Descriptions.Item>
    </Descriptions>
  )
}

function StepBusinessOutput({
  stepId,
  output,
  evidence,
}: {
  stepId: string
  output: string
  evidence: EvidenceItem[]
}) {
  const data = safeParse(output)
  switch (stepId) {
    case 'resolve_company':
      return (
        <Descriptions bordered size="small" column={2} className="business-descriptions">
          <Descriptions.Item label="识别主体">{text(data.normalized_name)}</Descriptions.Item>
          <Descriptions.Item label="置信度">{text(data.confidence)}</Descriptions.Item>
        </Descriptions>
      )
    case 'collect_materials':
      return (
        <AnalysisBox
          title="资料收集"
          summary={`已收集 ${text(data.material_count)} 份资料，来源：${text(data.source_type)}`}
          evidenceIds={asStringArray(data.created_evidence_ids)}
          evidence={evidence}
        />
      )
    case 'extract_basic_facts':
      return <CompanyFactsPanel data={data} />
    case 'calculate_financial_ratios':
      return <FinancialRatiosPanel data={data} />
    case 'financial_analysis':
      return <FinancialAnalysisPanel data={data} evidence={evidence} />
    case 'industry_analysis':
      return <IndustryAnalysisPanel data={data} />
    case 'risk_analysis':
      return <RiskAnalysisPanel data={data} evidence={evidence} />
    case 'rating_committee':
      return <RatingOpinionPanel data={data} />
    case 'devil_review':
      return <DevilReviewPanel data={data} />
    case 'human_review':
      return (
        <AnalysisBox
          title="人工审核"
          summary={`状态：${text(data.status)}；意见：${text(data.comment)}`}
          evidenceIds={[]}
          evidence={evidence}
        />
      )
    case 'generate_report':
      return <AnalysisBox title="报告生成" summary={text(data.report_name)} evidenceIds={[]} evidence={evidence} />
    default:
      return <JsonBlock value={data} compact />
  }
}

function CompanyFactsPanel({ data }: { data: Record<string, unknown> }) {
  const profile = objectOf(data.company_profile)
  const financials = objectOf(data.financials)
  return (
    <div className="business-panel">
      <Descriptions bordered size="small" column={1} className="business-descriptions">
        <Descriptions.Item label="公司名称">{text(profile.company_name)}</Descriptions.Item>
        <Descriptions.Item label="报告期间">{text(profile.period)}</Descriptions.Item>
        <Descriptions.Item label="业务摘要">{text(profile.business_summary)}</Descriptions.Item>
      </Descriptions>
      <KeyValueGrid
        items={[
          ['营业收入', money(financials.revenue)],
          ['净利润', money(financials.net_profit)],
          ['总资产', money(financials.total_assets)],
          ['总负债', money(financials.total_liabilities)],
          ['流动资产', money(financials.current_assets)],
          ['流动负债', money(financials.current_liabilities)],
        ]}
      />
    </div>
  )
}

function FinancialRatiosPanel({ data }: { data: Record<string, unknown> }) {
  const ratios = objectOf(data.ratios)
  return (
    <KeyValueGrid
      items={[
        ['资产负债率', ratio(ratios.asset_liability_ratio)],
        ['流动比率', ratio(ratios.current_ratio)],
        ['净利率', ratio(ratios.net_margin)],
      ]}
    />
  )
}

function FinancialAnalysisPanel({ data, evidence }: { data: Record<string, unknown>; evidence: EvidenceItem[] }) {
  return (
    <AnalysisBox title="财务分析" summary={text(data.summary)} evidenceIds={[]} evidence={evidence}>
      <ClaimList title="优势" claims={asRecordArray(data.strengths)} evidence={evidence} />
      <ClaimList title="弱项" claims={asRecordArray(data.weaknesses)} evidence={evidence} />
      <SimpleList title="不确定事项" items={asStringArray(data.uncertainties)} />
      <Tag color="orange">风险等级：{text(data.risk_level)}</Tag>
    </AnalysisBox>
  )
}

function IndustryAnalysisPanel({ data }: { data: Record<string, unknown> }) {
  return (
    <AnalysisBox title="行业与经营" summary={text(data.summary)} evidenceIds={[]} evidence={[]}>
      <Tag color="blue">行业位置：{text(data.industry_position)}</Tag>
      <SimpleList title="机会" items={asStringArray(data.opportunities)} />
      <SimpleList title="风险" items={asStringArray(data.risks)} />
      <SimpleList title="不确定事项" items={asStringArray(data.uncertainties)} />
    </AnalysisBox>
  )
}

function RiskAnalysisPanel({ data, evidence }: { data: Record<string, unknown>; evidence: EvidenceItem[] }) {
  return (
    <AnalysisBox title="风险分析" summary={text(data.summary)} evidenceIds={[]} evidence={evidence}>
      <Tag color="red">综合风险：{text(data.overall_risk_level)}</Tag>
      <div className="risk-list">
        {asRecordArray(data.risks).map((risk, index) => (
          <div className="risk-item" key={`${text(risk.risk_type)}-${index}`}>
            <strong>{text(risk.risk_type)}</strong>
            <Tag color="volcano">{text(risk.severity)}</Tag>
            <p>{text(risk.reason)}</p>
            <EvidenceRefs ids={asStringArray(risk.evidence_ids)} evidence={evidence} />
          </div>
        ))}
      </div>
      <SimpleList title="不确定事项" items={asStringArray(data.uncertainties)} />
    </AnalysisBox>
  )
}

function RatingOpinionPanel({ data }: { data: Record<string, unknown> }) {
  return (
    <AnalysisBox title="初步评级意见" summary={text(data.summary)} evidenceIds={[]} evidence={[]}>
      <Space wrap>
        <Tag color="green">评级方向：{text(data.rating_direction)}</Tag>
        <Tag color={data.requires_human_review ? 'gold' : 'green'}>
          人工审核：{data.requires_human_review ? '需要' : '不需要'}
        </Tag>
      </Space>
      <SimpleList title="支持因素" items={asStringArray(data.supporting_factors)} />
      <SimpleList title="约束因素" items={asStringArray(data.constraint_factors)} />
    </AnalysisBox>
  )
}

function DevilReviewPanel({ data }: { data: Record<string, unknown> }) {
  return (
    <AnalysisBox title="反方审查" summary={text(data.summary)} evidenceIds={[]} evidence={[]}>
      <SimpleList title="反方异议" items={asStringArray(data.objections)} />
      <SimpleList title="缺失证据" items={asStringArray(data.missing_evidence)} />
      <SimpleList title="建议复核点" items={asStringArray(data.suggested_review_points)} />
    </AnalysisBox>
  )
}

function ReviewTable({
  reviews,
  evidence,
  busy,
  onAction,
}: {
  reviews: ReviewItem[]
  evidence: EvidenceItem[]
  busy: boolean
  onAction: (action: 'approve' | 'reject' | 'modify', review: ReviewItem) => Promise<void>
}) {
  if (reviews.length === 0) {
    return <Empty description="暂无审核项" />
  }

  return (
    <div className="review-stack">
      {reviews.map((review) => {
        const content = safeParse(review.contentJson)
        const workflowContext = objectOf(content.workflow_context)
        const steps = objectOf(workflowContext.steps)
        return (
          <section className="review-panel" key={review.id}>
            <div className="review-head">
              <div>
                <Typography.Title level={4}>{review.title}</Typography.Title>
                <Space wrap>
                  <StatusTag status={review.status} />
                  <span className="muted">{formatDate(review.createdAt)}</span>
                </Space>
              </div>
              {review.status === 'Pending' ? (
                <Space wrap>
                  <Button
                    type="primary"
                    icon={<CheckCircleOutlined />}
                    loading={busy}
                    onClick={() => void onAction('approve', review)}
                  >
                    通过
                  </Button>
                  <Button loading={busy} onClick={() => void onAction('modify', review)}>
                    修改后通过
                  </Button>
                  <Button danger loading={busy} onClick={() => void onAction('reject', review)}>
                    驳回
                  </Button>
                </Space>
              ) : null}
            </div>

            <div className="review-grid">
              <section>
                <Typography.Title level={5}>评级意见</Typography.Title>
                <RatingOpinionPanel data={objectOf(steps.rating_committee)} />
              </section>
              <section>
                <Typography.Title level={5}>反方审查</Typography.Title>
                <DevilReviewPanel data={objectOf(steps.devil_review)} />
              </section>
            </div>

            <section className="surface">
              <Typography.Title level={5}>审核重点</Typography.Title>
              <SimpleList
                title="需要确认"
                items={[
                  '评级方向是否被财务、行业和风险证据支撑',
                  '反方审查列出的缺失证据是否会影响结论',
                  '报告是否仍应保持内部草稿而非正式评级',
                ]}
              />
              <EvidenceRefs ids={collectEvidenceIds(steps)} evidence={evidence} />
            </section>
          </section>
        )
      })}
    </div>
  )
}

function EvidenceTable({
  evidence,
  busy,
  onVerify,
}: {
  evidence: EvidenceItem[]
  busy: boolean
  onVerify: (evidenceId: string) => Promise<void>
}) {
  const columns: ColumnsType<EvidenceItem> = [
    { title: '来源', dataIndex: 'sourceName', width: 180 },
    { title: '章节', dataIndex: 'sectionTitle', width: 150 },
    { title: '摘录', dataIndex: 'quoteText', render: (value?: string) => <span className="quote">{value}</span> },
    { title: '置信度', dataIndex: 'confidence', width: 110, render: (value: number) => value.toFixed(2) },
    {
      title: '状态',
      dataIndex: 'verified',
      width: 120,
      render: (verified: boolean) => (verified ? <Tag color="green">Verified</Tag> : <Tag>Unchecked</Tag>),
    },
    {
      title: '操作',
      width: 100,
      render: (_, item) =>
        item.verified ? null : (
          <Button size="small" loading={busy} onClick={() => void onVerify(item.id)}>
            标记
          </Button>
        ),
    },
  ]

  return <Table rowKey="id" columns={columns} dataSource={evidence} pagination={false} scroll={{ x: 920 }} />
}

function ArtifactTable({ artifacts, onOpenReport }: { artifacts: Artifact[]; onOpenReport: () => void }) {
  const columns: ColumnsType<Artifact> = [
    { title: '名称', dataIndex: 'name' },
    { title: '类型', dataIndex: 'artifactType', width: 120, render: (value: string) => <Tag>{value}</Tag> },
    { title: '版本', dataIndex: 'version', width: 90 },
    { title: '时间', dataIndex: 'createdAt', width: 170, render: formatDate },
    {
      title: '操作',
      width: 120,
      render: (_, item) =>
        item.artifactType === 'Markdown' ? (
          <Button size="small" icon={<FileMarkdownOutlined />} onClick={onOpenReport}>
            预览
          </Button>
        ) : null,
    },
  ]

  return <Table rowKey="id" columns={columns} dataSource={artifacts} pagination={false} />
}

function TraceTable({ trace }: { trace: TraceEvent[] }) {
  const columns: ColumnsType<TraceEvent> = [
    { title: '事件', dataIndex: 'eventType', width: 180, render: (value: string) => <Tag color="blue">{value}</Tag> },
    { title: '消息', dataIndex: 'message' },
    { title: '时间', dataIndex: 'createdAt', width: 170, render: formatDate },
  ]

  return <Table rowKey="id" columns={columns} dataSource={trace} pagination={false} />
}

function StatusTag({ status }: { status: string }) {
  const color =
    status === 'Succeeded' || status === 'Approved' || status === 'Modified'
      ? 'green'
      : status === 'Failed' || status === 'Rejected'
        ? 'red'
        : status === 'WaitingReview' || status === 'Pending'
          ? 'gold'
          : status === 'Running'
            ? 'cyan'
            : 'default'

  return <Tag color={color}>{status}</Tag>
}

function JsonBlock({ value, compact = false }: { value: unknown; compact?: boolean }) {
  const content = typeof value === 'string' ? safeParse(value) : value
  return <pre className={compact ? 'json-block compact' : 'json-block'}>{JSON.stringify(content, null, 2)}</pre>
}

function AnalysisBox({
  title,
  summary,
  evidenceIds,
  evidence,
  children,
}: {
  title: string
  summary: string
  evidenceIds: string[]
  evidence: EvidenceItem[]
  children?: ReactNode
}) {
  return (
    <div className="analysis-box">
      <div className="analysis-title">{title}</div>
      <p>{summary}</p>
      {children}
      <EvidenceRefs ids={evidenceIds} evidence={evidence} />
    </div>
  )
}

function ClaimList({
  title,
  claims,
  evidence,
}: {
  title: string
  claims: Record<string, unknown>[]
  evidence: EvidenceItem[]
}) {
  if (claims.length === 0) {
    return null
  }

  return (
    <div className="claim-list">
      <strong>{title}</strong>
      {claims.map((claim, index) => (
        <div className="claim-item" key={`${text(claim.claim)}-${index}`}>
          <span>{text(claim.claim)}</span>
          <Tag>{ratio(claim.confidence)}</Tag>
          <EvidenceRefs ids={asStringArray(claim.evidence_ids)} evidence={evidence} />
        </div>
      ))}
    </div>
  )
}

function SimpleList({ title, items }: { title: string; items: string[] }) {
  if (items.length === 0) {
    return null
  }

  return (
    <div className="simple-list">
      <strong>{title}</strong>
      <ul>
        {items.map((item, index) => (
          <li key={`${item}-${index}`}>{item}</li>
        ))}
      </ul>
    </div>
  )
}

function EvidenceRefs({ ids, evidence }: { ids: string[]; evidence: EvidenceItem[] }) {
  const uniqueIds = [...new Set(ids.filter(Boolean))]
  if (uniqueIds.length === 0) {
    return null
  }

  return (
    <div className="evidence-refs">
      {uniqueIds.map((id) => {
        const item = evidence.find((candidate) => candidate.id === id)
        return (
          <Tag color={item?.verified ? 'green' : 'blue'} key={id}>
            {item?.sectionTitle ? `${item.sectionTitle}: ` : ''}
            {id.slice(0, 8)}
          </Tag>
        )
      })}
    </div>
  )
}

function KeyValueGrid({ items }: { items: Array<[string, string]> }) {
  return (
    <div className="kv-grid">
      {items.map(([label, value]) => (
        <div className="kv-item" key={label}>
          <span>{label}</span>
          <strong>{value}</strong>
        </div>
      ))}
    </div>
  )
}

function safeParse(value?: string | null): Record<string, unknown> {
  if (!value) {
    return {}
  }

  try {
    return JSON.parse(value) as Record<string, unknown>
  } catch {
    return { value }
  }
}

function objectOf(value: unknown): Record<string, unknown> {
  return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : {}
}

function asRecordArray(value: unknown): Record<string, unknown>[] {
  return Array.isArray(value) ? value.map(objectOf) : []
}

function asStringArray(value: unknown): string[] {
  return Array.isArray(value) ? value.map((item) => text(item)) : []
}

function text(value: unknown) {
  if (value === null || value === undefined || value === '') {
    return '未提供'
  }

  if (typeof value === 'object') {
    return JSON.stringify(value)
  }

  return String(value)
}

function money(value: unknown) {
  const numeric = Number(value)
  return Number.isFinite(numeric) ? `${(numeric / 100000000).toFixed(2)} 亿元` : '未提取'
}

function ratio(value: unknown) {
  const numeric = Number(value)
  return Number.isFinite(numeric) ? numeric.toFixed(4) : '未计算'
}

function collectEvidenceIds(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.flatMap(collectEvidenceIds)
  }

  if (!value || typeof value !== 'object') {
    return []
  }

  const record = value as Record<string, unknown>
  const ownIds = Array.isArray(record.evidence_ids) ? asStringArray(record.evidence_ids) : []
  return [...ownIds, ...Object.values(record).flatMap(collectEvidenceIds)]
}

function getMarkdown(artifact?: Artifact) {
  const content = safeParse(artifact?.contentJson)
  return typeof content.markdown === 'string' ? content.markdown : ''
}

function timelineColor(status: string) {
  if (status === 'Succeeded') {
    return 'green'
  }
  if (status === 'Failed') {
    return 'red'
  }
  if (status === 'WaitingReview') {
    return 'gold'
  }
  return 'blue'
}

function formatDate(value?: string | null) {
  return value ? dayjs(value).format('MM-DD HH:mm:ss') : '-'
}

function showError(error: unknown) {
  message.error(error instanceof Error ? error.message : '请求失败')
}

export default App
