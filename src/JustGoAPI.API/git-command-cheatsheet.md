# Git Commands: Details and Options (Most to Least Used)

---

## 1. Working Directory & Staging

### `git status`
- **Shows**: Working directory and staging state (what’s changed, staged, untracked).
- **Example:**  
  ```bash
  git status
  ```

### `git add .` / `git add <file>`
- **Stages changes** for commit (all or a specific file).
- **Example:**  
  ```bash
  git add .         # all files
  git add main.js   # just main.js
  ```

### `git commit -m "message"`
- **Records staged changes** to repository with a message.
- **`-m`**: Write inline commit message.
- **Example:**  
  ```bash
  git commit -m "Initial commit"
  ```

### `git push`
- **Uploads commits** from current branch to remote.
- **Example:**  
  ```bash
  git push               # push current branch
  git push origin develop # push develop branch
  ```

### `git pull`
- **Downloads and merges** changes from remote to local.
- **Example:**  
  ```bash
  git pull
  ```

---

## 2. Viewing History

### `git log`, `git log --oneline`, `git log --stat`
- **Shows commit history**.  
- **`--oneline`**: Each commit in one line.
- **`--stat`**: Shows what files/lines changed.
- **Example:**  
  ```bash
  git log
  git log --oneline
  git log --stat
  ```

### `git diff` / `git diff <file>` / `git diff --cached`
- **Compares changes** not staged (`git diff`), a file, or staged (`--cached`).
- **Example:**  
  ```bash
  git diff
  git diff login.js
  git diff --cached
  ```

### `git show <commit>`
- **Shows details and changes** for a specific commit.
- **Example:**  
  ```bash
  git show 70c1e3d
  ```

---

## 3. Branching and Merging

### `git branch`, `git branch -d <branch>`, `git branch -m <name>`
- **`git branch`**: List local branches.
- **`-d`**: Delete branch (if merged).
- **`-m`**: Rename branch.
- **Example:**  
  ```bash
  git branch        
  git branch -d bugfix
  git branch -m new-feature
  ```

### `git checkout <branch>`, `git checkout -b <branch>`
- **Switches** to a branch.
- **`-b <branch>`**: Create and switch to new branch.
- **Example:**  
  ```bash
  git checkout develop
  git checkout -b experiment
  ```

### `git merge <branch>`
- **Integrates changes** from another branch into current branch.
- **Example:**  
  ```bash
  git merge feature/cart
  ```

---

## 4. Synchronizing with Remote

### `git fetch`
- **Downloads** new commits/branches from remote WITHOUT merging.
- **Example:**  
  ```bash
  git fetch
  ```

### `git remote -v`
- **List**: Remotes (and URLs) for repository.
- **`-v`**: Verbose, show URLs.
- **Example:**  
  ```bash
  git remote -v
  ```

---

## 5. Temporary Changes

### `git stash`, `git stash list`, `git stash apply`, `git stash pop`, `git stash drop`
- **`git stash`**: Save uncommitted changes.
- **`list`**: See stashes.
- **`apply`**: Apply latest/specific stash (keeps stash).
- **`pop`**: Apply and remove from stash list.
- **`drop`**: Remove specific stash.
- **Example:**  
  ```bash
  git stash
  git stash list
  git stash apply
  git stash pop
  git stash drop
  ```

---

## 6. Restoring & Undoing

### `git checkout -- <file>`, `git checkout .`
- **Restores** file(s) to last commit (discard changes).
- **`-- <file>`**: Explicit restore file.
- **`.`**: All files in directory.
- **Example:**  
  ```bash
  git checkout -- style.css
  git checkout .
  ```

### `git reset --hard origin/<branch>`, `git reset --soft HEAD~1`
- **`--hard`**: Reset working directory and index to remote branch.
- **`--soft`**: Undo last commit, keep changes staged.
- **Example:**  
  ```bash
  git reset --hard origin/main
  git reset --soft HEAD~1
  ```

### `git reset <file>`
- **Un-stage** a file from index.
- **Example:**  
  ```bash
  git reset index.js
  ```

---

## 7. Tagging

### `git tag`, `git tag <name>`, `git tag --list`, `git push --tags`
- **List/Create/Push tags** (useful for releases).
- **`--list`**: Shows all tags.
- **`--tags`**: Push all tags to remote.
- **Example:**  
  ```bash
  git tag
  git tag v1.0.0
  git tag --list
  git push --tags
  ```

---

## 8. Repository Configuration

### `git config --list`
- **Lists all config** settings (user.name, user.email, editor, etc.).
- **`--list`**: Show every key/value config pair Git sees.
- **Example:**  
  ```bash
  git config --list
  ```

---

## 9. Advanced Operations

### `git rebase <branch>`, `git rebase --continue`, `git rebase --abort`
- **Move/rewrite** commits to new base.  
- **`--continue`**: Resume rebase after conflict.
- **`--abort`**: Cancel and restore pre-rebase state.
- **Example:**  
  ```bash
  git rebase dev
  git rebase --continue
  git rebase --abort
  ```

### `git cherry-pick <commit>`
- **Apply** one specific commit to current branch.
- **Example:**  
  ```bash
  git cherry-pick b7ec7ff
  ```

### `git remote add <name> <url>`, `git remote remove <name>`, `git remote set-url <name> <url>`
- **Add/Remove/Modify** remotes.
- **Example:**  
  ```bash
  git remote add upstream https://github.com/other/repo.git
  git remote remove upstream
  git remote set-url origin git@github.com:user/repo.git
  ```

---

# Option/Keyword Purposes at-a-Glance

| Option            | Purpose/Effect                                                                            |
|-------------------|------------------------------------------------------------------------------------------|
| `--list`          | Show all matching items (config, tags, remotes, etc)                                     |
| `-m`              | Provide a commit message inline                                                          |
| `-d`              | Delete (branches, tags, stashes, etc)                                                    |
| `-b`              | Create branch in checkout                                                                |
| `--oneline`       | Show log entries compactly                                                               |
| `--stat`          | Show files/lines changed in log output                                                   |
| `--cached`        | Diff what’s staged (vs HEAD)                                                             |
| `--hard`          | Discard all uncommitted work (beware: no recovery!)                                      |
| `--soft`          | Undo commits but keep changes staged                                                     |
| `--continue`      | Resume operation (after conflict resolution, e.g. during rebase or merge)                |
| `--abort`         | Abort operation (restore pre-rebase/pre-merge state)                                     |
| `--tags`          | Push all tags to remote                                                                  |
| `-v`              | “Verbose” - show more info (e.g., show URLs for remotes)                                 |
| `-p`              | In log: show patch/diff details                                                          |
| `--help`/`-h`     | Show help for the command                                                                |
| `--`              | Disambiguate file name from branch name (in checkout etc), or pass through more arguments|

---

# END
