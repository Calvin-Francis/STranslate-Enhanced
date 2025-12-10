# GitHub MCP Server 待改进需求

> 基于 2025-12-11 实际测试发现的问题

---

## 一、Bug 修复

### 1.1 upload_release_asset URL 解析错误

| 项目 | 内容 |
|------|------|
| **问题描述** | 上传 Release 附件时 URL 解析失败 |
| **错误信息** | `uploadURL must have a trailing slash, but "https://uploads.github.com" does not` |
| **根本原因** | URL 被截断，只取到了域名部分，丢失了完整路径 |
| **优先级** | P0 |

**正确的 URL 格式：**
```
https://uploads.github.com/repos/{owner}/{repo}/releases/{id}/assets{?name,label}
```

**修复建议：**
```typescript
// 从 release 响应中获取完整 upload_url
const uploadUrl = release.upload_url;
// 替换模板参数
const url = uploadUrl.replace('{?name,label}', `?name=${encodeURIComponent(fileName)}`);
```

---

### 1.2 sync_local_repository 遗漏图片文件

| 项目 | 内容 |
|------|------|
| **问题描述** | 同步本地仓库时，`images/` 目录未被同步 |
| **影响范围** | 所有二进制文件（.png, .jpg, .gif 等）可能被遗漏 |
| **复现步骤** | 调用 `sync_local_repository` 同步包含 images 目录的项目 |
| **优先级** | P0 |

**排查结果：**
- `.gitignore` 未排除 images 目录 ✅
- `git ls-files` 显示图片已被跟踪 ✅
- `git check-ignore` 确认未被忽略 ✅
- **结论**：问题在 MCP 工具实现层面

**可能原因：**
1. 默认只同步文本文件，排除二进制文件
2. 文件扩展名白名单/黑名单过滤
3. MIME 类型检测导致 image/* 被跳过
4. 目录遍历逻辑问题

**修复建议：**
- 检查文件过滤逻辑，确保不排除图片等二进制文件
- 或添加 `include_binary` 参数控制行为

---

## 二、功能增强

### 2.1 批量提交支持细粒度 Commit Message

| 项目 | 内容 |
|------|------|
| **问题描述** | `sync_local_repository` 只支持单一 `message` 参数 |
| **当前行为** | 所有文件使用相同的 commit message |
| **期望行为** | 允许为不同目录/文件指定不同的 commit message |
| **优先级** | P1 |

**当前参数设计：**
```typescript
sync_local_repository {
  message: string  // ← 只有一个，所有文件共用
}
```

**改进方案 A：多 Commit 配置**
```typescript
sync_local_repository {
  // 支持按模式分组，每组独立 commit
  commits?: Array<{
    patterns: string[];   // glob 模式，如 ["src/Controls/**"]
    message: string;      // 该组文件的 commit message
  }>;
  default_message?: string; // 未匹配文件的默认 message
}
```

**使用示例：**
```typescript
sync_local_repository({
  owner: "user",
  repo: "project", 
  branch: "main",
  commits: [
    { patterns: ["src/**/*.cs"], message: "feat: 新增屏幕覆盖翻译功能" },
    { patterns: ["images/**"], message: "assets: 添加收款码图片" },
    { patterns: ["*.md"], message: "docs: 更新文档" }
  ],
  default_message: "chore: 其他文件更新"
})
```

**改进方案 B：自动分组 + 模板**
```typescript
sync_local_repository {
  group_by?: 'directory' | 'extension' | 'none';
  message_template?: string;  // 如 "sync({directory}): 更新文件"
}
```

**效果：**
```
commit 1: "sync(src/Controls): 更新文件"
commit 2: "sync(src/Views): 更新文件"
commit 3: "sync(images): 更新文件"
```

---

## 三、测试报告

### 已测试功能

| 功能 | 状态 | 备注 |
|------|------|------|
| `create_release` | ✅ 通过 | 完美工作 |
| `get_release` | ✅ 通过 | - |
| `get_release_by_tag` | ✅ 通过 | - |
| `get_latest_release` | ✅ 通过 | - |
| `list_releases` | ✅ 通过 | - |
| `list_release_assets` | ✅ 通过 | - |
| `delete_release` | ✅ 通过 | - |
| `upload_release_asset` | ❌ 失败 | URL 解析错误 |
| `delete_directory` | ✅ 通过 | 一次删除 469 个文件 |
| `delete_files` | ✅ 通过 | 支持 force 参数 |
| `sync_local_repository` | ⚠️ 部分通过 | 遗漏 images 目录 |
| `push_files` | ✅ 通过 | 支持本地文件路径 |
| `push_directory` | ❌ 失败 | 报 "no files to push" |

---

## 四、待改进清单

| # | 问题 | 类型 | 优先级 | 状态 |
|---|------|------|--------|------|
| 1 | upload_release_asset URL 解析 | Bug | P0 | 待修复 |
| 2 | sync_local_repository 遗漏图片 | Bug | P0 | 待修复 |
| 3 | **push_files 损坏二进制文件** | Bug | **P0** | 待修复 |
| 4 | 批量提交细粒度 message | 功能增强 | P1 | 待实现 |
| 5 | push_directory 二进制文件支持 | Bug | P1 | 待修复 |

### Bug #3 详情：push_files 损坏二进制文件

**验证方式：**
```powershell
# 本地文件哈希
Get-FileHash ".\images\alipay.png"
# DE3BB5285AF2A38738BBE0CC629AE82057602F56...

# 远程文件哈希（下载后）
# B6D95DAD0B1D9AF28B1A6B07DE4C3AF80D5B6D24...

# 不匹配！文件被损坏
```

**根本原因：** 二进制文件被当作文本处理，导致字节被错误转换

---

*文档更新时间：2025-12-11 06:41*
*基于实际测试整理*
