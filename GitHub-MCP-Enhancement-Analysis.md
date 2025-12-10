# GitHub MCP Server 功能增强可行性分析

> 基于实际使用过程中的痛点，分析 github-mcp-server 的优化空间

---

## 一、缺失功能分析

### 1.1 Release 管理功能

#### create_release

| 项目 | 内容 |
|------|------|
| **需求场景** | 自动化发布版本，CI/CD 集成 |
| **GitHub API** | `POST /repos/{owner}/{repo}/releases` |
| **可行性** | ⭐⭐⭐⭐⭐ 完全可行 |
| **实现难度** | 低 |

**参数设计：**
```typescript
interface CreateReleaseParams {
  owner: string;           // 仓库所有者
  repo: string;            // 仓库名
  tag_name: string;        // 标签名，如 "v2.0.0"
  name?: string;           // Release 标题
  body?: string;           // 描述内容（支持 Markdown）
  target_commitish?: string; // 目标分支/commit，默认主分支
  draft?: boolean;         // 是否草稿，默认 false
  prerelease?: boolean;    // 是否预发布，默认 false
  generate_release_notes?: boolean; // 自动生成变更日志
}
```

**返回值：**
```typescript
interface ReleaseResponse {
  id: number;
  tag_name: string;
  name: string;
  html_url: string;        // Release 页面链接
  upload_url: string;      // 附件上传地址
}
```

---

#### upload_release_asset

| 项目 | 内容 |
|------|------|
| **需求场景** | 上传构建产物（ZIP、安装包、二进制文件） |
| **GitHub API** | `POST {upload_url}` (从 release 获取) |
| **可行性** | ⭐⭐⭐⭐ 可行，需处理大文件 |
| **实现难度** | 中 |

**参数设计：**
```typescript
interface UploadReleaseAssetParams {
  owner: string;
  repo: string;
  release_id: number;      // 或 tag_name
  file_path: string;       // 本地文件路径
  name?: string;           // 上传后的文件名，默认使用原文件名
  label?: string;          // 显示标签
}
```

**技术要点：**
- 需要读取本地文件并上传
- 支持大文件（GitHub 限制单个文件 2GB）
- Content-Type 自动识别
- 上传进度回调（可选）

---

#### delete_release

| 项目 | 内容 |
|------|------|
| **需求场景** | 删除错误发布、清理旧版本 |
| **GitHub API** | `DELETE /repos/{owner}/{repo}/releases/{release_id}` |
| **可行性** | ⭐⭐⭐⭐⭐ 完全可行 |
| **实现难度** | 低 |

---

#### delete_release_asset

| 项目 | 内容 |
|------|------|
| **需求场景** | 删除错误上传的附件 |
| **GitHub API** | `DELETE /repos/{owner}/{repo}/releases/assets/{asset_id}` |
| **可行性** | ⭐⭐⭐⭐⭐ 完全可行 |
| **实现难度** | 低 |

---

### 1.2 仓库内容管理增强

#### delete_directory（批量删除）

| 项目 | 内容 |
|------|------|
| **需求场景** | 清空仓库、删除整个目录 |
| **GitHub API** | 需遍历目录逐个删除文件 |
| **可行性** | ⭐⭐⭐ 可行但有限制 |
| **实现难度** | 中 |

**技术要点：**
- GitHub API 不支持直接删除目录
- 需要递归获取目录内容，逐个删除文件
- 删除最后一个文件会导致仓库变空（需特殊处理）
- 建议使用 Git Data API 创建空 tree 来清空

**替代方案：**
```typescript
// 使用 Git Data API 直接操作 tree
// 1. 获取当前 commit 的 tree
// 2. 创建新的空/修改后的 tree
// 3. 创建新 commit 指向新 tree
// 4. 更新 ref 指向新 commit
```

---

#### push_directory（递归推送目录）

| 项目 | 内容 |
|------|------|
| **需求场景** | 推送整个项目目录 |
| **GitHub API** | Git Data API (trees/blobs/commits) |
| **可行性** | ⭐⭐⭐⭐ 可行 |
| **实现难度** | 高 |

**技术要点：**
- 使用 Git Data API 而非 Contents API
- 支持 .gitignore 过滤
- 批量创建 blobs
- 构建完整 tree
- 创建 commit 并更新 ref

**参数设计：**
```typescript
interface PushDirectoryParams {
  owner: string;
  repo: string;
  branch: string;
  local_path: string;       // 本地目录路径
  remote_path?: string;     // 远程目录路径，默认根目录
  message: string;          // commit 消息
  respect_gitignore?: boolean; // 是否遵守 .gitignore，默认 true
  max_file_size?: number;   // 单文件大小限制 (bytes)
}
```

---

## 二、现有功能优化

### 2.1 sync_local_repository 增强

**当前问题：**
- 硬限制 500 文件，大项目无法使用
- 全量同步，效率低

**优化建议：**

| 优化项 | 可行性 | 说明 |
|--------|--------|------|
| 提高文件限制 | ⭐⭐⭐⭐ | 可配置参数，默认 500，最大 5000 |
| 增量同步 | ⭐⭐⭐ | 需要本地保存上次同步状态 |
| .gitignore 支持 | ⭐⭐⭐⭐⭐ | 使用 ignore 库解析 |
| 并行上传 | ⭐⭐⭐⭐ | 提升大量小文件的同步速度 |

**改进后参数：**
```typescript
interface SyncLocalRepositoryParams {
  // ... 现有参数
  max_files?: number;        // 默认 500，最大 5000
  max_file_size?: number;    // 单文件大小限制，默认 100MB
  respect_gitignore?: boolean; // 默认 true
  include_patterns?: string[]; // 包含模式
  exclude_patterns?: string[]; // 排除模式
  parallel_uploads?: number;   // 并行上传数，默认 5
}
```

---

### 2.2 push_files 增强

**当前问题：**
- 只能推送文件列表，不支持目录
- 大文件处理不友好

**优化建议：**

```typescript
interface PushFilesParams {
  // ... 现有参数
  files: Array<{
    path: string;
    content?: string;        // 文本内容
    file_path?: string;      // 或本地文件路径（二选一）
    encoding?: 'utf-8' | 'base64'; // 编码方式
  }>;
}
```

---

### 2.3 delete_file 增强

**当前问题：**
- 无法删除仓库最后一个文件
- 不支持批量删除

**优化建议：**
- 添加 `force` 参数允许清空仓库
- 添加 `delete_files`（复数）支持批量操作

---

## 三、新增辅助功能

### 3.1 get_repository_size

| 项目 | 内容 |
|------|------|
| **需求场景** | 同步前评估仓库大小 |
| **GitHub API** | `GET /repos/{owner}/{repo}` (size 字段) |
| **可行性** | ⭐⭐⭐⭐⭐ 完全可行 |
| **实现难度** | 低 |

---

### 3.2 compare_commits

| 项目 | 内容 |
|------|------|
| **需求场景** | 查看两个版本间的差异 |
| **GitHub API** | `GET /repos/{owner}/{repo}/compare/{base}...{head}` |
| **可行性** | ⭐⭐⭐⭐⭐ 完全可行 |
| **实现难度** | 低 |

---

### 3.3 create_tag

| 项目 | 内容 |
|------|------|
| **需求场景** | 创建版本标签（配合 release） |
| **GitHub API** | `POST /repos/{owner}/{repo}/git/tags` |
| **可行性** | ⭐⭐⭐⭐⭐ 完全可行 |
| **实现难度** | 低 |

---

## 四、优先级排序

### P0 - 必须实现
1. `create_release` - 发布版本核心功能
2. `upload_release_asset` - 上传构建产物

### P1 - 高优先级
3. `delete_release` - Release 管理完整性
4. `push_directory` - 大项目支持
5. `sync_local_repository` 增强 - 提高文件限制

### P2 - 中优先级
6. `delete_directory` - 批量清理
7. `delete_release_asset` - 附件管理
8. `create_tag` - 标签管理

### P3 - 低优先级
9. `get_repository_size` - 辅助功能
10. `compare_commits` - 辅助功能

---

## 五、技术实现参考

### GitHub API 认证
```typescript
const headers = {
  'Authorization': `Bearer ${GITHUB_TOKEN}`,
  'Accept': 'application/vnd.github+json',
  'X-GitHub-Api-Version': '2022-11-28'
};
```

### Release 相关 API 端点
```
POST   /repos/{owner}/{repo}/releases              # 创建 release
GET    /repos/{owner}/{repo}/releases              # 列出 releases
GET    /repos/{owner}/{repo}/releases/{id}         # 获取单个 release
PATCH  /repos/{owner}/{repo}/releases/{id}         # 更新 release
DELETE /repos/{owner}/{repo}/releases/{id}         # 删除 release
POST   {upload_url}                                # 上传附件
GET    /repos/{owner}/{repo}/releases/{id}/assets  # 列出附件
DELETE /repos/{owner}/{repo}/releases/assets/{id}  # 删除附件
```

### 上传附件示例
```typescript
async function uploadReleaseAsset(
  uploadUrl: string,
  filePath: string,
  fileName: string
) {
  const fileContent = await fs.readFile(filePath);
  const contentType = mime.lookup(fileName) || 'application/octet-stream';
  
  // uploadUrl 格式: https://uploads.github.com/repos/{owner}/{repo}/releases/{id}/assets{?name,label}
  const url = uploadUrl.replace('{?name,label}', `?name=${encodeURIComponent(fileName)}`);
  
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': contentType,
      'Content-Length': fileContent.length.toString()
    },
    body: fileContent
  });
  
  return response.json();
}
```

---

## 六、总结

| 类别 | 数量 | 可行性评估 |
|------|------|-----------|
| 新增 Release 功能 | 4 个 | 全部可行 |
| 内容管理增强 | 2 个 | 大部分可行 |
| 现有功能优化 | 3 个 | 全部可行 |
| 辅助功能 | 3 个 | 全部可行 |

**总体结论：** 所有优化项技术上均可行，主要工作量在于：
1. Release 附件上传的大文件处理
2. 目录递归操作的性能优化
3. .gitignore 解析集成

---

*文档创建时间：2025-12-11*
*基于 github-mcp-server 使用经验整理*
