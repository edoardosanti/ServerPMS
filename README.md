# ServerPMS


*workflow*
✅ 3. Clone the Repository Locally
Each collaborator should clone the repo on their machine:
git clone https://github.com/your-username/your-repo-name.git
cd your-repo-name


✅ 4. Create a Branch for Each Feature or Bugfix
Always use branches to avoid breaking the main codebase:

git checkout -b feature/login-page
Make your changes, then commit:

git add .
git commit -m "Add login page UI"
Push the branch to GitHub:

git push origin feature/login-page


✅ 5. Open a Pull Request (PR)
On GitHub, go to the repo.
GitHub will show your pushed branch and offer to open a Pull Request (PR).
Describe what you changed.
Your collaborator can review, comment, request changes, or merge it.

✅ 6. Review and Merge
Discuss changes in the PR thread.
When ready, merge the branch into main (or master).
After merging, delete the branch (optional but clean).

✅ 7. Sync with Main Branch
Periodically, make sure your local main is up-to-date:

git checkout main
git pull origin main

✅ 8. Resolve Conflicts (if any)
If both collaborators change the same code, Git might report conflicts during merging. You'll need to manually edit the conflicting files, then:

git add conflicted_file
git commit

✅ 9. Use Issues, Projects, and Wiki
GitHub also helps with planning:
- *Issues:* Track bugs, tasks, ideas.
- *Projects:* Organize tasks Kanban-style.
- *Wiki:* Document the app (e.g., setup steps, API specs).
