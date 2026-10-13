import { Globe, HeartPulse, Landmark, LayoutDashboard, Settings2, SmilePlus, type LucideIcon } from 'lucide-react';

const AREA_ICON: Record<string, LucideIcon> = {
  EXECUTIVE_SUMMARY: LayoutDashboard,
  VAS: Globe,
  OPERATIONS: Settings2,
  FINANCE: Landmark,
  ALFIT: HeartPulse,
  CSAT: SmilePlus,
};

/** The icon for a reporting area. An area the frontend does not know yet gets the dashboard icon. */
export function areaIcon(code: string): LucideIcon {
  return AREA_ICON[code] ?? LayoutDashboard;
}
