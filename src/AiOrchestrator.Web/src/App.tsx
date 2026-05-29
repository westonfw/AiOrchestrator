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
  Drawer,
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
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
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
                                  {step.outputJson ? <JsonBlock value={step.outputJson} /> : null}
                                </div>
                              ),
                            }))}
                          />
                        </section>
                        <section className="surface">
                          <Typography.Title level={4}>任务输入</Typography.Title>
                          <JsonBlock value={selectedInput} />
                        </section>
                      </div>
                    ),
                  },
                  {
                    key: 'review',
                    label: '审核',
                    children: <ReviewTable reviews={detail.reviews} busy={busy} onAction={handleReview} />,
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
        <pre className="markdown-preview">{markdown}</pre>
      </Modal>
    </Layout>
  )
}

function ReviewTable({
  reviews,
  busy,
  onAction,
}: {
  reviews: ReviewItem[]
  busy: boolean
  onAction: (action: 'approve' | 'reject' | 'modify', review: ReviewItem) => Promise<void>
}) {
  const columns: ColumnsType<ReviewItem> = [
    { title: '标题', dataIndex: 'title', width: 220 },
    { title: '状态', dataIndex: 'status', width: 120, render: (status: string) => <StatusTag status={status} /> },
    { title: '时间', dataIndex: 'createdAt', width: 170, render: formatDate },
    {
      title: '内容',
      dataIndex: 'contentJson',
      render: (value: string) => <JsonBlock value={value} compact />,
    },
    {
      title: '操作',
      width: 210,
      render: (_, review) =>
        review.status === 'Pending' ? (
          <Space>
            <Button
              type="primary"
              size="small"
              icon={<CheckCircleOutlined />}
              loading={busy}
              onClick={() => void onAction('approve', review)}
            >
              通过
            </Button>
            <Button size="small" loading={busy} onClick={() => void onAction('modify', review)}>
              修改
            </Button>
            <Button danger size="small" loading={busy} onClick={() => void onAction('reject', review)}>
              驳回
            </Button>
          </Space>
        ) : null,
    },
  ]

  return <Table rowKey="id" columns={columns} dataSource={reviews} pagination={false} scroll={{ x: 980 }} />
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
