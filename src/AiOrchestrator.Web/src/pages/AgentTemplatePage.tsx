import {
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  RobotOutlined,
} from '@ant-design/icons'
import {
  Button,
  Col,
  Drawer,
  Form,
  Input,
  InputNumber,
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
  createAgentTemplate,
  deleteAgentTemplate,
  getAgentTemplate,
  listAgentTemplates,
  listSkills,
  updateAgentTemplate,
} from '../api'
import type { SaveAgentTemplatePayload } from '../api'
import type { AgentTemplateSummary, SkillInfo } from '../types'

const { TextArea } = Input
const { Text } = Typography

const DEFAULT_SCHEMA = JSON.stringify(
  { type: 'object', required: ['summary'], properties: { summary: { type: 'string' } } },
  null,
  2,
)

export default function AgentTemplatePage() {
  const [agents, setAgents] = useState<AgentTemplateSummary[]>([])
  const [skills, setSkills] = useState<SkillInfo[]>([])
  const [loading, setLoading] = useState(false)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editId, setEditId] = useState<string | null>(null)
  const [form] = Form.useForm<SaveAgentTemplatePayload>()
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [a, s] = await Promise.all([listAgentTemplates(), listSkills()])
      setAgents(a.items)
      setSkills(s.items)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void load() }, [load])

  const openCreate = () => {
    setEditId(null)
    form.setFieldsValue({
      scenarioCode: '',
      agentCode: '',
      name: '',
      description: '',
      model: 'default',
      temperature: 0.2,
      systemPrompt: '',
      outputSchemaJson: DEFAULT_SCHEMA,
      allowedSkills: [],
      allowedDataSources: [],
      maxToolCalls: 10,
    })
    setDrawerOpen(true)
  }

  const openEdit = async (id: string) => {
    setEditId(id)
    setDrawerOpen(true)
    try {
      const agent = await getAgentTemplate(id)
      let schemaText = agent.outputSchemaJson
      try { schemaText = JSON.stringify(JSON.parse(agent.outputSchemaJson), null, 2) } catch { /* keep as-is */ }
      form.setFieldsValue({
        scenarioCode: agent.scenarioCode,
        agentCode: agent.agentCode,
        name: agent.name,
        description: agent.description,
        model: agent.model,
        temperature: agent.temperature,
        systemPrompt: agent.systemPrompt,
        outputSchemaJson: schemaText,
        allowedSkills: agent.allowedSkills,
        allowedDataSources: agent.allowedDataSources,
        maxToolCalls: agent.maxToolCalls,
      })
    } catch (err) {
      void message.error(String(err))
    }
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setBusy(true)
    try {
      if (editId) {
        await updateAgentTemplate(editId, values)
        void message.success('Agent 已更新')
      } else {
        await createAgentTemplate(values)
        void message.success('Agent 已创建')
      }
      setDrawerOpen(false)
      await load()
    } catch (err) {
      void message.error(String(err))
    } finally {
      setBusy(false)
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteAgentTemplate(id)
      void message.success('已删除')
      await load()
    } catch (err) {
      void message.error(String(err))
    }
  }

  const columns: ColumnsType<AgentTemplateSummary> = [
    { title: '场景', dataIndex: 'scenarioCode', width: 120 },
    { title: 'Code', dataIndex: 'agentCode', width: 160, render: v => <Text code>{v}</Text> },
    { title: '名称', dataIndex: 'name' },
    { title: '描述', dataIndex: 'description', ellipsis: true },
    { title: '模型', dataIndex: 'model', width: 140 },
    { title: 'Temp', dataIndex: 'temperature', width: 70 },
    {
      title: '更新',
      dataIndex: 'updatedAt',
      width: 160,
      render: v => new Date(v).toLocaleString(),
    },
    {
      title: '操作',
      width: 100,
      render: (_, row) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => void openEdit(row.id)} />
          <Popconfirm title="确认删除?" onConfirm={() => void handleDelete(row.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <Space style={{ marginBottom: 16 }} align="center">
        <RobotOutlined style={{ fontSize: 20 }} />
        <Typography.Title level={4} style={{ margin: 0 }}>Agent 模板</Typography.Title>
        <Button icon={<PlusOutlined />} type="primary" onClick={openCreate}>新建 Agent</Button>
        <Button onClick={() => void load()}>刷新</Button>
      </Space>

      <Table
        rowKey="id"
        dataSource={agents}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 20 }}
      />

      <Drawer
        title={editId ? '编辑 Agent' : '新建 Agent'}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={720}
        extra={
          <Space>
            <Button onClick={() => setDrawerOpen(false)}>取消</Button>
            <Button type="primary" loading={busy} onClick={() => void handleSave()}>保存</Button>
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="scenarioCode" label="场景 Code" rules={[{ required: true }]}>
                <Input placeholder="credit_rating" disabled={!!editId} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="agentCode" label="Agent Code" rules={[{ required: true }]}>
                <Input placeholder="my_analyst" disabled={!!editId} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={16}>
              <Form.Item name="name" label="名称" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="model" label="模型" rules={[{ required: true }]}>
                <Input placeholder="default / gpt-4.1-mini" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="description" label="描述">
            <Input />
          </Form.Item>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="temperature" label="Temperature">
                <InputNumber min={0} max={2} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="maxToolCalls" label="最大工具调用次数">
                <InputNumber min={1} max={50} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="allowedSkills" label="允许调用的 Skill">
            <Select mode="multiple" placeholder="选择 Skill">
              {skills.map(s => (
                <Select.Option key={s.code} value={s.code}>
                  {s.name} <Text type="secondary">({s.code})</Text>
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="allowedDataSources"
            label={<span>允许访问的数据源 <Tag color="blue">预留</Tag></span>}
          >
            <Select mode="tags" placeholder="输入数据源 Code（暂无可选项）" />
          </Form.Item>
          <Form.Item name="systemPrompt" label="System Prompt" rules={[{ required: true }]}>
            <TextArea rows={10} placeholder="你是一名财务分析师..." />
          </Form.Item>
          <Form.Item name="outputSchemaJson" label="输出 Schema（JSON Schema）" rules={[{ required: true }]}>
            <TextArea rows={12} style={{ fontFamily: 'monospace', fontSize: 12 }} />
          </Form.Item>
        </Form>
      </Drawer>
    </div>
  )
}
