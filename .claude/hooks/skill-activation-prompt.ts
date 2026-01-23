#!/usr/bin/env node
import { readFileSync } from 'fs';
import { join } from 'path';

interface HookInput {
    session_id: string;
    transcript_path: string;
    cwd: string;
    permission_mode: string;
    prompt: string;
}

interface WorkflowTriggers {
    keywords?: string[];
    intentPatterns?: string[];
}

interface Workflow {
    name: string;
    description: string;
    priority: 'critical' | 'high' | 'medium' | 'low';
    sequence: string[];
    triggers: WorkflowTriggers;
}

interface SkillRules {
    version: string;
    workflows: Record<string, Workflow>;
}

interface MatchedWorkflow {
    id: string;
    workflow: Workflow;
    matchType: 'keyword' | 'intent';
}

async function main() {
    try {
        // Read input from stdin
        const input = readFileSync(0, 'utf-8');
        const data: HookInput = JSON.parse(input);
        const prompt = data.prompt.toLowerCase();

        // Load skill rules
        const projectDir = process.env.CLAUDE_PROJECT_DIR || data.cwd;
        const rulesPath = join(projectDir, '.claude', 'skills', 'skill-rules.json');

        let rules: SkillRules;
        try {
            rules = JSON.parse(readFileSync(rulesPath, 'utf-8'));
        } catch {
            // No rules file, exit silently
            process.exit(0);
        }

        // Check if we have workflows (v2) or skills (v1)
        if (!rules.workflows) {
            process.exit(0);
        }

        const matchedWorkflows: MatchedWorkflow[] = [];

        // Check each workflow for matches
        for (const [workflowId, workflow] of Object.entries(rules.workflows)) {
            const triggers = workflow.triggers;
            if (!triggers) {
                continue;
            }

            // Keyword matching
            if (triggers.keywords) {
                const keywordMatch = triggers.keywords.some(kw =>
                    prompt.includes(kw.toLowerCase())
                );
                if (keywordMatch) {
                    matchedWorkflows.push({ id: workflowId, workflow, matchType: 'keyword' });
                    continue;
                }
            }

            // Intent pattern matching
            if (triggers.intentPatterns) {
                const intentMatch = triggers.intentPatterns.some(pattern => {
                    const regex = new RegExp(pattern, 'i');
                    return regex.test(prompt);
                });
                if (intentMatch) {
                    matchedWorkflows.push({ id: workflowId, workflow, matchType: 'intent' });
                }
            }
        }

        // Generate output if matches found
        if (matchedWorkflows.length > 0) {
            // Sort by priority
            const priorityOrder = { critical: 0, high: 1, medium: 2, low: 3 };
            matchedWorkflows.sort((a, b) =>
                priorityOrder[a.workflow.priority] - priorityOrder[b.workflow.priority]
            );

            let output = '\n';
            output += '┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓\n';
            output += '┃  🎯 SKILL WORKFLOW ACTIVATION                    ┃\n';
            output += '┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛\n\n';

            // Group by priority
            const critical = matchedWorkflows.filter(w => w.workflow.priority === 'critical');
            const high = matchedWorkflows.filter(w => w.workflow.priority === 'high');
            const medium = matchedWorkflows.filter(w => w.workflow.priority === 'medium');

            const formatWorkflow = (mw: MatchedWorkflow, icon: string) => {
                let result = `${icon} ${mw.workflow.name}\n`;
                result += `   ${mw.workflow.description}\n`;
                result += `   ┌─────────────────────────────────────────────\n`;
                result += `   │ SEQUENCE:\n`;
                mw.workflow.sequence.forEach((skill, idx) => {
                    const isFirst = idx === 0;
                    const arrow = isFirst ? '▶' : '→';
                    const skillShort = skill.replace('superpowers:', '');
                    result += `   │  ${idx + 1}. ${arrow} ${skillShort}${isFirst ? ' ← START HERE' : ''}\n`;
                });
                result += `   └─────────────────────────────────────────────\n\n`;
                return result;
            };

            if (critical.length > 0) {
                output += '⚠️  CRITICAL WORKFLOW (MUST FOLLOW):\n\n';
                critical.forEach(mw => {
                    output += formatWorkflow(mw, '🔴');
                });
            }

            if (high.length > 0) {
                output += '📚 RECOMMENDED WORKFLOW:\n\n';
                high.forEach(mw => {
                    output += formatWorkflow(mw, '🟡');
                });
            }

            if (medium.length > 0) {
                output += '💡 SUGGESTED WORKFLOW:\n\n';
                medium.forEach(mw => {
                    output += formatWorkflow(mw, '🟢');
                });
            }

            // Get the first skill from the highest priority workflow
            const primaryWorkflow = matchedWorkflows[0];
            const firstSkill = primaryWorkflow.workflow.sequence[0];

            output += '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n';
            output += `⚡ ACTION: Invoke Skill tool with "${firstSkill}"\n`;
            output += '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n';

            console.log(output);
        }

        process.exit(0);
    } catch (err) {
        // Fail silently to not block user
        process.exit(0);
    }
}

main().catch(() => {
    process.exit(0);
});
