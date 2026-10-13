import type { MenuItem } from '@/features/session/api/sessionRepository';
import { HomeScreen } from '@/features/home/HomeScreen';

export default function HomePage({ menu }: { menu: MenuItem[] }) {
  return <HomeScreen menu={menu} />;
}
