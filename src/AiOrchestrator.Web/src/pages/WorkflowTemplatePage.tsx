import {
  ApartmentOutlined,
  CheckCircleOutlined,
  CopyOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  ThunderboltOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Col,
  Drawer,
  Form,
  Input,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { useCallback, useEffect, useState } from 'react'
import {
  activateWorkflowTemplate,
  cloneWorkflowFromYaml,
  createWorkflowTemplate,
  deleteWorkflowTemplate,
  getWorkflowTemplate,
  listAgentTemplates,
  listSkills,
  listWorkflowTemplates,
  updateWorkflowTemplate,
} from '../api'
import type {
  AgentTemplateSummary,
  SkillInfo,
  WorkflowTemplateSummary,
} from '../types'
import type { SaveWorkflowTemplatePayload, WorkflowStepTemplatePayload } from '../api'

const { Text } = Typography

type StepForm = {
  stepId: string
  name: string
  type: 'skill' | 'agent' | 'review'
  skillCode?: string
  agentCode?: string
  dependsOn: string[]
}

export default function WorkflowTemplatePage() {
  const [templates, setTemplates] = useState<WorkflowTemplateSummary[]>([])
  const [skills, setSkills] = useState<SkillInfo[]>([])
  const [agents, setAgents] = useState<AgentTemplateSummary[]>([])
  const [loading, setLoading] = useState(false)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editId, setEditId] = useState<string | null>(null)
  const [editSteps, setEditSteps] = useState<StepForm[]>([])
  const [headerForm] = Form.useForm<{ scenarioCode: string; name: string; version: string }>()
  const [busy, setBusy] = useState(false)
  const [cloneModalOpen, setCloneModalOpen] = useState(false)
  const [cloneScenario, setCloneScenario] = useState('credit_rating')
  const [dagError, setDagError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [t, s, a] = await Promise.all([
        listWorkflowTemplates(),
        listSkills(),
        listAgentTemplates(),
      ])
      setTemplates(t.items)
      setSkills(s.items)
      setAgents(a.items)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void load() }, [load])

  const openCreate = () => {
    setEditId(null)
    setEditSteps([])
    setDagError(null)
    headerForm.setFieldsValue({ scenarioCode: '', name: '', version: '1.0.0' })
    setDrawerOpen(true)
  }

  const openEdit = async (id: string) => {
    setEditId(id)
    setDrawerOpen(true)
    setDagError(null)
    try {
      const tpl = await getWorkflowTemplate(id)
      headerForm.setFieldsValue({
        scenarioCode: tpl.scenarioCode,
        name: tpl.name,
        version: tpl.version,
      })
      setEditSteps(
        tpl.steps
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map(s => ({
            stepId: s.stepId,
            name: s.name,
            type: s.type as StepForm['type'],
            skillCode: s.skillCode ?? undefined,
            agentCode: s.agentCode ?? undefined,
            dependsOn: s.dependsOn,
          })),
      )
    } catch (err) {
      void message.error(String(err))
    }
  }

  const validateDag = (steps: StepForm[]): string | null => {
    const ids = new Set(steps.map(s => s.stepId))
    for (const s of steps) {
      for (const dep of s.dependsOn) {
        if (!ids.has(dep)) return `步骤 "${s.stepId}" 依赖不存在的步骤 "${dep}"`
      }
    }
    const graph = new Map(steps.map(s => [s.stepId, s.dependsOn]))
    const visited = new Set<string>()
    const inStack = new Set<string>()
    const hasCycle = (node: string): boolean => {
      if (inStack.has(node)) return true
      if (visited.has(node)) return false
      visited.add(node); inStack.add(node)
      for (const dep of graph.get(node) ?? []) if (hasCycle(dep)) return true
      inStack.delete(node)
      return false
    }
    if (steps.some(s => hasCycle(s.stepId))) return '步骤依赖存在循环'
    return null
  }

  const handleSave = async () => {
    const header = await headerForm.validateFields()
    const err = validateDag(editSteps)
    if (err) { setDagError(err); return }
    setDagError(null)
    setBusy(true)
    try {
      const payload: SaveWorkflowTemplatePayload = {
        ...header,
        inputSchemaJson: '{}',
        steps: editSteps.map((s, i): WorkflowStepTemplatePayload => ({
          stepId: s.stepId,
          name: s.name,
          type: s.type,
          skillCode: s.skillCode ?? null,
          agentCode: s.agentCode ?? null,
          dependsOn: s.dependsOn,
          sortOrder: i,
        })),
      }
      if (editId) {
        await updateWorkflowTemplate(editId, payload)
        void message.success('已保存')
      } else {
        await createWorkflowTemplate(payload)
        void message.success('已创建')
      }
      setDrawerOpen(false)
      await load()
    } catch (err) {
      void message.error(String(err))
    } finally {
      setBusy(false)
    }
  }

  const handleActivate = async (id: string) => {
    try {
      await activateWorkflowTemplate(id)
      void message.success('已激活')
      await load()
    } catch (err) {
      void message.error(String(err))
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteWorkflowTemplate(id)
      void message.success('已删除')
      await load()
    } catch (err) {
      void message.error(String(err))
    }
  }

  const handleClone = async () => {
    setBusy(true)
    try {
      const { id } = await cloneWorkflowFromYaml(cloneScenario)
      void message.success(`已导入为副本，ID: ${id}`)
      setCloneModalOpen(false)
      await load()
      await openEdit(id)
    } catch (err) {
      void message.error(String(err))
    } finally {
      setBusy(false)
    }
  }

  // ── Step editor helpers ────────────────────────────────────────────────────

  const addStep = () =>
    setEditSteps(prev => [
      ...prev,
      { stepId: `step_${prev.length + 1}`, name: '新步骤', type: 'skill', dependsOn: [] },
    ])

  const removeStep = (idx: number) =>
    setEditSteps(prev => prev.filter((_, i) => i !== idx))

  const updateStep = (idx: number, patch: Partial<StepForm>) =>
    setEditSteps(prev => prev.map((s, i) => (i === idx ? { ...s, ...patch } : s)))

  // ── Table columns ──────────────────────────────────────────────────────────

  const columns: ColumnsType<WorkflowTemplateSummary> = [
    { title: '场景', dataIndex: 'scenarioCode', width: 120 },
    {
      title: '名称',
      dataIndex: 'name',
      render: (v, row) => (
        <Space>
          {v}
          {row.isActive && <Tag color="green" icon={<CheckCircleOutlined />}>激活</Tag>}
        </Space>
      ),
    },
    { title: '版本', dataIndex: 'version', width: 80 },
    { title: '步骤数', dataIndex: 'stepCount', width: 70 },
    { title: '更新', dataIndex: 'updatedAt', width: 160, render: v => new Date(v).toLocaleString() },
    {
      title: '操作',
      width: 200,
      render: (_, row) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => void openEdit(row.id)}>编辑</Button>
          {!row.isActive && (
            <Popconfirm title="激活后将替换当前生产流程" onConfirm={() => void handleActivate(row.id)}>
              <Button size="small" icon={<ThunderboltOutlined />} type="primary">激活</Button>
            </Popconfirm>
          )}
          {!row.isActive && (
            <Popconfirm title="确认删除？此操作不可撤销" onConfirm={() => void handleDelete(row.id)}>
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  // ── Step row columns (inside drawer) ──────────────────────────────────────

  const stepColumns: ColumnsType<StepForm & { _idx: number }> = [
    {
      title: 'Step ID',
      width: 140,
      render: (_, row) => (
        <Input
          size="small"
          value={row.stepId}
          onChange={e => updateStep(row._idx, { stepId: e.target.value })}
        />
      ),
    },
    {
      title: '名称',
      width: 140,
      render: (_, row) => (
        <Input
          size="small"
          value={row.name}
          onChange={e => updateStep(row._idx, { name: e.target.value })}
        />
      ),
    },
    {
      title: '类型',
      width: 100,
      render: (_, row) => (
        <Select
          size="small"
          value={row.type}
          style={{ width: '100%' }}
          onChange={v => updateStep(row._idx, { type: v as StepForm['type'], skillCode: undefined, agentCode: undefined })}
          options={[
            { value: 'skill', label: 'Skill' },
            { value: 'agent', label: 'Agent' },
            { value: 'review', label: '人工审核' },
          ]}
        />
      ),
    },
    {
      title: 'Skill / Agent',
      render: (_, row) => {
        if (row.type === 'skill') {
          return (
            <Select
              size="small"
              value={row.skillCode}
              style={{ width: '100%' }}
              allowClear
              placeholder="选择 Skill"
              onChange={v => updateStep(row._idx, { skillCode: v })}
            >
              {skills.map(s => <Select.Option key={s.code} value={s.code}>{s.name}</Select.Option>)}
            </Select>
          )
        }
        if (row.type === 'agent') {
          return (
            <Select
              size="small"
              value={row.agentCode}
              style={{ width: '100%' }}
              allowClear
              placeholder="选择 Agent"
              onChange={v => updateStep(row._idx, { agentCode: v })}
            >
              {agents.map(a => <Select.Option key={a.agentCode} value={a.agentCode}>{a.name}</Select.Option>)}
            </Select>
          )
        }
        return <Text type="secondary">—</Text>
      },
    },
    {
      title: '依赖步骤',
      render: (_, row) => (
        <Select
          size="small"
          mode="multiple"
          value={row.dependsOn}
          style={{ width: '100%' }}
          placeholder="选择前置步骤"
          onChange={v => updateStep(row._idx, { dependsOn: v })}
        >
          {editSteps
            .filter((_, i) => i !== row._idx)
            .map(s => <Select.Option key={s.stepId} value={s.stepId}>{s.stepId}</Select.Option>)}
        </Select>
      ),
    },
    {
      title: '',
      width: 40,
      render: (_, row) => (
        <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeStep(row._idx)} />
      ),
    },
  ]

  return (
    <div>
      <Space style={{ marginBottom: 16 }} align="center">
        <ApartmentOutlined style={{ fontSize: 20 }} />
        <Typography.Title level={4} style={{ margin: 0 }}>工作流模板</Typography.Title>
        <Button icon={<PlusOutlined />} type="primary" onClick={openCreate}>新建模板</Button>
        <Button icon={<CopyOutlined />} onClick={() => setCloneModalOpen(true)}>从内置场景导入</Button>
        <Button onClick={() => void load()}>刷新</Button>
      </Space>

      <Table
        rowKey="id"
        dataSource={templates}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 20 }}
      />

      {/* Clone from YAML modal */}
      <Modal
        title="从内置场景导入"
        open={cloneModalOpen}
        onOk={() => void handleClone()}
        onCancel={() => setCloneModalOpen(false)}
        confirmLoading={busy}
      >
        <Form layout="vertical">
          <Form.Item label="场景 Code">
            <Input value={cloneScenario} onChange={e => setCloneScenario(e.target.value)} placeholder="credit_rating" />
          </Form.Item>
        </Form>
        <Text type="secondary">将把 YAML 文件中的流程和步骤克隆成可编辑副本，不影响原始文件。</Text>
      </Modal>

      {/* Workflow editor drawer */}
      <Drawer
        title={editId ? '编辑工作流模板' : '新建工作流模板'}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={900}
        extra={
          <Space>
            <Button onClick={() => setDrawerOpen(false)}>取消</Button>
            <Button type="primary" loading={busy} onClick={() => void handleSave()}>保存</Button>
          </Space>
        }
      >
        <Form form={headerForm} layout="vertical" style={{ marginBottom: 16 }}>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="scenarioCode" label="场景 Code" rules={[{ required: true }]}>
                <Input placeholder="credit_rating" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="name" label="模板名称" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col span={4}>
              <Form.Item name="version" label="版本">
                <Input placeholder="1.0.0" />
              </Form.Item>
            </Col>
          </Row>
        </Form>

        <Space style={{ marginBottom: 8 }}>
          <Text strong>步骤列表</Text>
          <Button size="small" icon={<PlusOutlined />} onClick={addStep}>添加步骤</Button>
        </Space>

        {dagError && <Alert type="error" message={dagError} style={{ marginBottom: 8 }} />}

        <Table
          rowKey="_idx"
          size="small"
          pagination={false}
          dataSource={editSteps.map((s, i) => ({ ...s, _idx: i }))}
          columns={stepColumns}
          scroll={{ x: 800 }}
        />
      </Drawer>
    </div>
  )
}
