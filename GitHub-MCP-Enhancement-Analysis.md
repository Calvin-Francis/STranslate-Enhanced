# GitHub MCP Server 待改进需求

> 基于 2025-12-11 实际测试

---

## 一、待实现功能

### 1.1 批量提交支持完全自定义 Commit Message

| 项目 | 内容 |
|------|------|
| **当前状态** | `group_by` + `message_template` 只能替换目录名占位符 |
| **当前效果** | `sync(src): 更新文件`、`sync(images): 更新文件` |
| **问题** | 描述文字相同，只有目录名不同，不够灵活 |
| **优先级** | P1 |

**期望效果：**
```
src/**    → "feat: 新增屏幕覆盖翻译功能"
images/** → "assets: 添加收款码图片"
*.md      → "docs: 更新文档"
其他      → "chore: 其他文件"
```

**参数设计：**
```typescript
sync_local_repository {
  // 方案：commits 数组，每组完全自定义 message
  commits?: Array<{
    patterns: string[];   // glob 模式
    message: string;      // 完整的 commit message（无占位符）
  }>;
  default_message?: string; // 未匹配文件的默认 message
}
```

**使用示例：**
```typescript
sync_local_repository({
  owner: "Calvin-Francis",
  repo: "STranslate-Enhanced",
  branch: "main",
  local_path: "c:/project",
  commits: [
    { 
      patterns: ["src/**/*.cs", "src/**/*.xaml"], 
      message: "feat: 新增屏幕覆盖翻译功能" 
    },
    { 
      patterns: ["images/**"], 
      message: "assets: 添加收款码图片" 
    },
    { 
      patterns: ["*.md"], 
      message: "docs: 更新文档" 
    }
  ],
  default_message: "chore: 其他文件更新"
})
```

**预期结果：**
```
commit 1: "feat: 新增屏幕覆盖翻译功能"    (src 下的 .cs/.xaml 文件)
commit 2: "assets: 添加收款码图片"         (images 目录)
commit 3: "docs: 更新文档"                 (.md 文件)
commit 4: "chore: 其他文件更新"            (其他未匹配文件)
```

---

## 二、已完成功能

| # | 功能 | 状态 |
|---|------|------|
| 1 | `create_release` | ✅ |
| 2 | `upload_release_asset` | ✅ |
| 3 | `delete_release` | ✅ |
| 4 | `push_files` 二进制文件支持 | ✅ |
| 5 | `push_directory` 二进制文件支持 | ✅ |
| 6 | `sync_local_repository` 图片同步 | ✅ |
| 7 | `delete_directory` 批量删除 | ✅ |
| 8 | `delete_files` 支持 force 参数 | ✅ |
| 9 | `group_by` + `message_template` | ✅ (有限支持) |

---

## 三、待改进清单

| # | 需求 | 类型 | 优先级 | 状态 |
|---|------|------|--------|------|
| 1 | **commits 数组参数** | 功能增强 | P1 | 待实现 |

### 详细说明

**当前 `group_by` 的局限性：**
- 只能按目录或扩展名自动分组
- message 只能用模板替换 `{directory}` 占位符
- 无法为特定模式指定完全不同的描述

**`commits` 参数的优势：**
- AI 可以根据文件内容分析，指定有意义的 commit message
- 支持 glob 模式匹配，灵活分组
- 每组完全独立的 message，无需占位符
- 更符合语义化提交规范 (Conventional Commits)

---

*文档更新时间：2025-12-11 07:14*
